using Microsoft.Data.Sqlite;

namespace PS4Remarry.Core;

public enum PkgSort
{
    TitleAsc, TitleDesc, TitleIdAsc, TitleIdDesc, GameThenUpdate, UpdateThenGame,
}

/// <summary>
/// SQLite store for the PKG library. Uses the same table layout as the old SFO-Info
/// tool, so an existing pkg.db can simply be copied into the tools folder.
/// All SQL is parameterised.
/// </summary>
public sealed class PkgCatalogDb
{
    public string DbPath { get; }
    private readonly string _connectionString;

    public PkgCatalogDb(string dbPath)
    {
        DbPath = Path.GetFullPath(dbPath);
        Directory.CreateDirectory(Path.GetDirectoryName(DbPath)!);
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = DbPath,
            Pooling = false,   // don't keep the file locked after we're done
        }.ToString();
        EnsureSchema();
    }

    private SqliteConnection Open()
    {
        var con = new SqliteConnection(_connectionString);
        con.Open();
        return con;
    }

    private void EnsureSchema()
    {
        using var con = Open();
        using var cmd = con.CreateCommand();
        cmd.CommandText =
            "CREATE TABLE IF NOT EXISTS PKG(id INTEGER, Title Text, Title_ID Text, Category Text, " +
            "Version Text, App_Version Text, Content_ID Text, Information Text)";
        cmd.ExecuteNonQuery();
    }

    private static string OrderBy(PkgSort s) => s switch
    {
        PkgSort.TitleAsc       => "Title COLLATE NOCASE ASC, Category COLLATE NOCASE ASC, Version COLLATE NOCASE ASC",
        PkgSort.TitleDesc      => "Title COLLATE NOCASE DESC, Version COLLATE NOCASE ASC",
        PkgSort.TitleIdAsc     => "Title_ID COLLATE NOCASE ASC, Category COLLATE NOCASE ASC",
        PkgSort.TitleIdDesc    => "Title_ID COLLATE NOCASE DESC, Category COLLATE NOCASE ASC",
        PkgSort.GameThenUpdate => "Category COLLATE NOCASE ASC, Title COLLATE NOCASE ASC",
        PkgSort.UpdateThenGame => "Category COLLATE NOCASE DESC, Title COLLATE NOCASE ASC",
        _                      => "Title COLLATE NOCASE ASC",
    };

    public List<PkgRecord> GetAll(PkgSort sort = PkgSort.TitleAsc) => Query(null, sort);

    /// <summary>Matches the text against title, title id, content id and the information box.</summary>
    public List<PkgRecord> Search(string text, PkgSort sort = PkgSort.TitleAsc) =>
        string.IsNullOrWhiteSpace(text) ? GetAll(sort) : Query(text.Trim(), sort);

    private List<PkgRecord> Query(string? search, PkgSort sort)
    {
        var list = new List<PkgRecord>();
        using var con = Open();
        using var cmd = con.CreateCommand();

        var sql = "SELECT id, Title, Title_ID, Category, Version, App_Version, Content_ID, Information FROM PKG";
        if (search != null)
        {
            sql += " WHERE Title LIKE $s ESCAPE '\\' OR Title_ID LIKE $s ESCAPE '\\' " +
                   "OR Content_ID LIKE $s ESCAPE '\\' OR Information LIKE $s ESCAPE '\\'";
            var escaped = search.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
            cmd.Parameters.AddWithValue("$s", "%" + escaped + "%");
        }
        cmd.CommandText = sql + " ORDER BY " + OrderBy(sort);

        using var rd = cmd.ExecuteReader();
        while (rd.Read())
        {
            list.Add(new PkgRecord
            {
                Id = rd.IsDBNull(0) ? 0 : rd.GetInt32(0),
                Title = Str(rd, 1),
                TitleId = Str(rd, 2),
                Category = Str(rd, 3),
                Version = Str(rd, 4),
                AppVersion = Str(rd, 5),
                ContentId = Str(rd, 6),
                Information = Str(rd, 7),
            });
        }
        return list;
    }

    private static string Str(SqliteDataReader rd, int i) => rd.IsDBNull(i) ? "" : rd.GetString(i);

    public int Insert(PkgRecord r)
    {
        using var con = Open();
        using var tx = con.BeginTransaction();
        int id = NextId(con, tx);
        InsertRow(con, tx, r, id);
        tx.Commit();
        r.Id = id;
        return id;
    }

    public void Update(PkgRecord r)
    {
        using var con = Open();
        using var cmd = con.CreateCommand();
        cmd.CommandText =
            "UPDATE PKG SET Title=$t, Title_ID=$tid, Category=$c, Version=$v, App_Version=$av, " +
            "Content_ID=$cid, Information=$i WHERE id=$id";
        cmd.Parameters.AddWithValue("$t", r.Title);
        cmd.Parameters.AddWithValue("$tid", r.TitleId);
        cmd.Parameters.AddWithValue("$c", r.Category);
        cmd.Parameters.AddWithValue("$v", r.Version);
        cmd.Parameters.AddWithValue("$av", r.AppVersion);
        cmd.Parameters.AddWithValue("$cid", r.ContentId);
        cmd.Parameters.AddWithValue("$i", r.Information ?? "");
        cmd.Parameters.AddWithValue("$id", r.Id);
        cmd.ExecuteNonQuery();
    }

    public void Delete(int id)
    {
        using var con = Open();
        using var cmd = con.CreateCommand();
        cmd.CommandText = "DELETE FROM PKG WHERE id=$id";
        cmd.Parameters.AddWithValue("$id", id);
        cmd.ExecuteNonQuery();
    }

    /// <summary>Applies a plan from <see cref="PkgImportPlanner"/> in one transaction.</summary>
    public void Apply(PkgImportPlan plan)
    {
        if (!plan.HasChanges) return;

        using var con = Open();
        using var tx = con.BeginTransaction();

        int next = NextId(con, tx);
        foreach (var r in plan.Inserts)
        {
            // Re-number in case the table changed since the plan was made.
            InsertRow(con, tx, r, next);
            r.Id = next++;
        }

        foreach (var kv in plan.InfoUpdates)
        {
            using var cmd = con.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = "UPDATE PKG SET Information=$i WHERE id=$id";
            cmd.Parameters.AddWithValue("$i", kv.Value);
            cmd.Parameters.AddWithValue("$id", kv.Key);
            cmd.ExecuteNonQuery();
        }

        tx.Commit();
    }

    private static int NextId(SqliteConnection con, SqliteTransaction tx)
    {
        using var cmd = con.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = "SELECT COALESCE(MAX(id), 0) FROM PKG";
        return Convert.ToInt32(cmd.ExecuteScalar()) + 1;
    }

    private static void InsertRow(SqliteConnection con, SqliteTransaction tx, PkgRecord r, int id)
    {
        using var cmd = con.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText =
            "INSERT INTO PKG(id, Title, Title_ID, Category, Version, App_Version, Content_ID, Information) " +
            "VALUES($id, $t, $tid, $c, $v, $av, $cid, $i)";
        cmd.Parameters.AddWithValue("$id", id);
        cmd.Parameters.AddWithValue("$t", r.Title);
        cmd.Parameters.AddWithValue("$tid", r.TitleId);
        cmd.Parameters.AddWithValue("$c", r.Category);
        cmd.Parameters.AddWithValue("$v", r.Version);
        cmd.Parameters.AddWithValue("$av", r.AppVersion);
        cmd.Parameters.AddWithValue("$cid", r.ContentId);
        cmd.Parameters.AddWithValue("$i", r.Information ?? "");
        cmd.ExecuteNonQuery();
    }
}

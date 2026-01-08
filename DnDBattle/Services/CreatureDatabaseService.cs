using DnDBattle.Models;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DnDBattle.Services
{
    public class CreatureDatabaseService : IDisposable
    {
        /*private const string DatabasePath = "Creatures.db";
        private const string ConnectionString = $"Data Source={DatabasePath};Version=3";

        public static void InitializeDatabase()
        {
            if (!File.Exists(DatabasePath))
            {
                SQLiteConnection.CreateFile(DatabasePath);
                using var conn = new SQLiteConnection(ConnectionString);
                conn.Open();

                    var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                CREATE TABLE Creatures (
                    Id TEXT PRIMARY KEY,
                    Name TEXT,
                    Type TEXT,
                    Size TEXT,
                    Alignment TEXT,
                    ChallengeRating TEXT,
                    ArmorClass INTEGER,
                    MaxHP INTEGER,
                    HitDice TEXT,
                    Speed TEXT,
                    Tags TEXT
                );
                ";
                cmd.ExecuteNonQuery();
            }
        }

        public static List<Token> LoadCreatures()
        {
            var creatures = new List<Token>();
            using var conn = new SQLiteConnection(ConnectionString);
            conn.Open();

            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM Creatures;";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var creature = new Token()
                {
                    Id = Guid.Parse(reader["Id"].ToString()),
                    Name = reader["Name"].ToString(),
                    Type = reader["Type"].ToString(),
                    Size = reader["Size"].ToString(),
                    Alignment = reader["Alignment"].ToString(),
                    ChallengeRating = reader["ChallengeRating"].ToString(),
                    ArmorClass = Convert.ToInt32(reader["ArmorClass"]),
                    MaxHP = Convert.ToInt32(reader["MaxHP"]),
                    HitDice = reader["HitDice"].ToString(),
                    Speed = reader["Speed"].ToString(),
                    Tags = new List<string>(reader["Tags"].ToString().Split(","))
                };
                creatures.Add(creature);
            }
            return creatures;
        }

        public static void SaveCreature(IEnumerable<Token> creatures)
        {
            using var conn = new SQLiteConnection(ConnectionString);
            conn.Open();

            var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM Creatures";
            cmd.ExecuteNonQuery();

            foreach (var creature in creatures)
            {
                cmd.CommandText = @"
                INSERT INTO Creatures (Id, Name, Type, Size, Alignment, ChallengeRating, ArmorClass, MaxHP, HitDice, Speed, Tags)
                VALUES (@Id, @Name, @Type, @Size, @Alignment, @ChallengeRating, @ArmorClass, @MaxHP, @HitDice, @Speed, @Tags);";
                cmd.Parameters.AddWithValue("@Id", creature.Id.ToString());
                cmd.Parameters.AddWithValue("@Name", creature.Name);
                cmd.Parameters.AddWithValue("@Type", creature.Type);
                cmd.Parameters.AddWithValue("@Size", creature.Size);
                cmd.Parameters.AddWithValue("@Alignment", creature.Alignment);
                cmd.Parameters.AddWithValue("@ChallengeRating", creature.ChallengeRating);
                cmd.Parameters.AddWithValue("@ArmorClass", creature.ArmorClass);
                cmd.Parameters.AddWithValue("@MaxHP", creature.MaxHP);
                cmd.Parameters.AddWithValue("@HitDice", creature.HitDice);
                cmd.Parameters.AddWithValue("@Speed", creature.Speed);
                cmd.Parameters.AddWithValue("@Tags", string.Join(",", creature.Tags));
                cmd.ExecuteNonQuery();
                cmd.Parameters.Clear();
            }
        }*/

        private readonly string _dbPath;
        private SqliteConnection _conn;

        public CreatureDatabaseService(string dbPath = "CreatureBank.db")
        {
            _dbPath = dbPath;
            InitializeDatabase();
        }

        #region Database Initialization

        private void InitializeDatabase()
        {
            _conn = new SqliteConnection($"Data Source={_dbPath}");
            _conn.Open();

            var createTablesCmd = _conn.CreateCommand();
            createTablesCmd.CommandText = @"
                -- Main Creatures table
                CREATE TABLE IF NOT EXISTS Creatures (
	                Id TEXT PRIMARY KEY,
	                Name TEXT NOT NULL,
	                Size TEXT,
	                Type TEXT,
	                Alignment TEXT,
	                ChallengeRating TEXT,
	                ArmorClass INTEGER,
	                MaxHP INTEGER,
	                HitDice TEXT,
	                InitiativeModifier INTEGER,
	                Speed TEXT,
	                Str INTEGER,
	                Dex INTEGER,
	                Con INTEGER,
	                Int INTEGER,
	                Wis INTEGER,
	                Cha INTEGER,
	                Skills, TEXT,
	                Senses TEXT,
	                Languages TEXT,
	                Immunities TEXT,
	                Resistances TEXT,
	                Vulnerabilities TEXT,
	                Traits TEXT,
	                Notes TEXT,
	                IconPath TEXT,
	                SizeInSquares INTEGER DEFAULT 1,
	                Category TEXT,
	                SourceFile TEXT,
	                DateAdded TEXT
	                );

	                -- Actions table (one-to-many with Creatures)
	                CREATE TABLE IF NOT EXISTS CreatureActions (
	                Id INTEGER PRIMARY KEY AUTOINCREMENT,
	                CreatureId TEXT NOT NULL,
	                ActionType TEXT NOT NULL,
	                Name TEXT,
	                AttackBonus INTEGER,
	                DamageExpression TEXT,
	                Range TEXT,
	                Description TEXT,
	                FOREIGN KEY (CreatureId) REFERENCES Creatures(Id) ON DELETE CASCADE
	                );

	                -- Tags table (many-to-many with Creatures)
	                CREATE TABLE IF NOT EXISTS Tags (
	                Id INTEGER PRIMARY KEY AUTOINCREMENT,
	                Name TEXT UNIQUE NOT NULL
	                );

	                -- Junction table for Creature-Tag relationship
	                CREATE TABLE IF NOT EXISTS CreatureTags (
	                CreatureId TEXT NOT NULL,
	                TagId INTEGER NOT NULL,
	                PRIMARY KEY (CreatureId, TagId),
	                FOREIGN KEY (CreatureId) REFERENCES Creatures(Id) ON DELETE CASCADE,
	                FOREIGN KEY (TagId) REFERENCES Tags(Id) ON DELETE CASCADE
	                );
	
	                -- Create indexes for faster searching
	                CREATE INDEX IF NOT EXISTS idx_creatures_name ON Creatures(Name);
	                CREATE INDEX IF NOT EXISTS idx_creatures_type ON Creatures(Type);
	                CREATE INDEX IF NOT EXISTS idx_creatures_category ON Creatures(Category);
	                CREATE INDEX IF NOT EXISTS idx_creatures_cr ON Creatures(ChallengeRating);
	                CREATE INDEX IF NOT EXISTS idx_tags_name ON Tags(Name);
                ";
            createTablesCmd.ExecuteNonQuery();
        }

        #endregion

        #region Creature CRUD Operations

        public async Task<int> AddCreatureAsync(Token creature, string category = null, string sourceFile = null)
        {
            using var transaction = _conn.BeginTransaction();

            try
            {
                var cmd = _conn.CreateCommand();
                cmd.CommandText = @"
                    INSERT OR REPLACE INTO Creatures
                    (Id, Name, Size, Type, Alignment, ChallengeRating, ArmorClass, MaxHP, HitDice,
                    InitiativeModifier, Speed, Str, Dex, Con, Int, Wis, Cha, Skills, Senses,
                    Languages, Immunities, Resistances, Vulnerabilities, Traits, Notes, IconPath,
                    SizeInSquares, Category, SourceFile, DateAdded)
                    VALUES
                    (@Id, @Name, @Size, @Type, @Alignment, @ChallengeRating, @ArmorClass, @MaxHP, @HitDice,
                    @InitiativeModifier, @Speed, @Str, @Dex, @Con, @Int, @Wis, @Cha, @Skills, @Senses,
                    @Languages, @Immunities, @Resistances, @Vulnerabilities, @Traits, @Notes, @IconPath,
                    @SizeInSquares, @Category, @SourceFile, @DateAdded)
                ";

                cmd.Parameters.AddWithValue("@Id", creature.Id.ToString());
                cmd.Parameters.AddWithValue("@Name", creature.Name ?? "");
                cmd.Parameters.AddWithValue("@Size", creature.Size ?? "");
                cmd.Parameters.AddWithValue("@Type", creature.Type ?? "");
                cmd.Parameters.AddWithValue("@Alignment", creature.Alignment ?? "");
                cmd.Parameters.AddWithValue("@ChallengeRating", creature.ChallengeRating ?? "");
                cmd.Parameters.AddWithValue("ArmorClass", creature.ArmorClass);
                cmd.Parameters.AddWithValue("@MaxHP", creature.MaxHP);
                cmd.Parameters.AddWithValue("@HitDice", creature.HitDice ?? "1d4");
                cmd.Parameters.AddWithValue("@InitiativeModifier", creature.InitiativeModifier);
                cmd.Parameters.AddWithValue("@Speed", creature.Speed ?? "walk 30, swim 15");
                cmd.Parameters.AddWithValue("@Str", creature.Str);
                cmd.Parameters.AddWithValue("@Dex", creature.Dex);
                cmd.Parameters.AddWithValue("@Con", creature.Con);
                cmd.Parameters.AddWithValue("@Int", creature.Int);
                cmd.Parameters.AddWithValue("@Wis", creature.Wis);
                cmd.Parameters.AddWithValue("@Cha", creature.Cha);
                cmd.Parameters.AddWithValue("@Skills", creature.Skills != null ? string.Join(",", creature.Skills) : "");
                cmd.Parameters.AddWithValue("@Languages", creature.Languages ?? "");
                cmd.Parameters.AddWithValue("@Immunities", creature.Immunities ?? "");
                cmd.Parameters.AddWithValue("@Resistances", creature.Resistances ?? "");
                cmd.Parameters.AddWithValue("@Vulnerabilities", creature.Vulnerabilities ?? "");
                cmd.Parameters.AddWithValue("@Traits", creature.Traits ?? "");
                cmd.Parameters.AddWithValue("@Notes", creature.Notes ?? "");
                cmd.Parameters.AddWithValue("@IconPath", creature.IconPath ?? "");
                cmd.Parameters.AddWithValue("@SizeInSquares", creature.SizeInSquares);
                cmd.Parameters.AddWithValue("@Category", category ?? "");
                cmd.Parameters.AddWithValue("@SourceFile", sourceFile ?? "");
                cmd.Parameters.AddWithValue("@DataAdded", DateTime.Now.ToString("O"));

                await cmd.ExecuteNonQueryAsync();

                var deleteActionCmd = _conn.CreateCommand();
                deleteActionCmd.CommandText = "DELETE FROM CreatureActions WHERE CreatureId = @CreatureId";
                deleteActionCmd.Parameters.AddWithValue("@CreatureId", creature.Id.ToString());
                await deleteActionCmd.ExecuteNonQueryAsync();

                await InsertActionsAsync(creature.Id.ToString(), "Action", creature.Actions);
                await InsertActionsAsync(creature.Id.ToString(), "BonusAction", creature.BonusActions);
                await InsertActionsAsync(creature.Id.ToString(), "Reaction", creature.Reactions);
                await InsertActionsAsync(creature.Id.ToString(), "LegendaryAction", creature.LegendaryActions);

                if (creature.Tags != null && creature.Tags.Count > 0)
                {
                    await SetCreatureTagsAsync(creature.Id.ToString(), creature.Tags);
                }

                transaction.Commit();
                return 1;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        private async Task InsertActionsAsync(string creatureId, string actionType, List<Models.Action> actions)
        {
            if (actions == null || actions.Count == 0) return;
            
            foreach (var action in actions)
            {
                var cmd = _conn.CreateCommand();
                cmd.CommandText = @"
                    INSERT INTO CreatureActions (CreatureId, ActionType, Name, AttackBonus, DamageExpression, Range, Description)
                    VALUE (@CreatureId, @ActionType, @Name, @AttackBonus, @DamageExpression, @Range, @Description)";

                cmd.Parameters.AddWithValue("@CreatureId", creatureId);
                cmd.Parameters.AddWithValue("@ActionType", actionType);
                cmd.Parameters.AddWithValue("@Name", action.Name ?? "");
                cmd.Parameters.AddWithValue("@AttackBonus", action.AttackBonus);
                cmd.Parameters.AddWithValue("@DamageExpression", action.DamageExpression ?? "");
                cmd.Parameters.AddWithValue("@Range", action.Range ?? "");
                cmd.Parameters.AddWithValue("@Description", action.Description ?? "");

                await cmd.ExecuteNonQueryAsync();
            }
        }

        public async Task<Token> GetCreatureByIdAsync(string id)
        {
            var cmd = _conn.CreateCommand();

            cmd.CommandText = "SELECT * FROM Creatures WHERE Id = @Id";
            cmd.Parameters.AddWithValue("@Id", id);

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var creature = MapReaderToToken(reader);
                creature.Actions = await GetActionsAsync(id, "Action");
                creature.BonusActions = await GetActionsAsync(id, "BonusAction");
                creature.Reactions = await GetActionsAsync(id, "Reactions");
                creature.LegendaryActions = await GetActionsAsync(id, "LegendaryActions");
                creature.Tags = await GetCreatureTagsAsync(id);
                return creature;
            }

            return null;
        }

        private async Task<List<Models.Action>> GetActionsAsync(string creatureId, string actionType)
        {
            var actions = new List<Models.Action>();

            var cmd = _conn.CreateCommand();
            cmd.CommandText = @"
                SELECT Name, AttackBonus, DamageExpression, Range, Description
                FROM CreatureActions
                WHERE CreatureId = @CreatureId AND ActionType = @ActionType";
            cmd.Parameters.AddWithValue("@CreatureId", creatureId);
            cmd.Parameters.AddWithValue("@ActionType", actionType);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                actions.Add(new Models.Action()
                {
                    Name = reader.GetString(0),
                    AttackBonus = reader.GetInt32(1),
                    DamageExpression = reader.GetString(2),
                    Range = reader.GetString(3),
                    Description = reader.GetString(4)
                });
            }

            return actions;
        }

        public async Task DeleteCreatureAsync(string id)
        {
            var cmd = _conn.CreateCommand();
            cmd.CommandText = "DELETE FROM Creatures WHERE Id = @Id";
            cmd.Parameters.AddWithValue("@Id", id);
            await cmd.ExecuteNonQueryAsync();
        }

        #endregion

        #region Tag Operations

        public async Task<int> GetOrCreateTagIdAsync(string tagName)
        {
            var getCmd = _conn.CreateCommand();
            getCmd.CommandText = "SELECT Id FROM Tags WHERE Name = @Name COLLATE NOCASE";
            getCmd.Parameters.AddWithValue("@Name", tagName.Trim());

            var result = await getCmd.ExecuteScalarAsync();
            if (result != null)
            {
                return Convert.ToInt32(result);
            }

            var insertCmd = _conn.CreateCommand();
            insertCmd.CommandText = "INSERT INTO Tags (Name) VALUES (@Name); SELECT last_insert_id();";
            insertCmd.Parameters.AddWithValue("@Name", tagName.Trim());

            return Convert.ToInt32(await insertCmd.ExecuteScalarAsync());
        }

        public async Task SetCreatureTagsAsync(string creatureId, List<string> tags)
        {
            var deleteCmd = _conn.CreateCommand();
            deleteCmd.CommandText = "DELETE FROM CreatureTags WHERE CreatureId = @CreatureId";
            deleteCmd.Parameters.AddWithValue("@CreatureId", creatureId);
            await deleteCmd.ExecuteNonQueryAsync();

            foreach (var tag in tags.Where(t => !string.IsNullOrWhiteSpace(t)))
            {
                var tagId = await GetOrCreateTagIdAsync(tag);

                var insertCmd = _conn.CreateCommand();
                insertCmd.CommandText = @"
                    INSERT OR IGNORE INTO CreatureTags (CreatureId, TagId)
                    VALUES (@CreatureId, @TagId)";
                insertCmd.Parameters.AddWithValue("@CreatureId", creatureId);
                insertCmd.Parameters.AddWithValue("@TagId", tagId);
                await insertCmd.ExecuteNonQueryAsync();
            }
        }

        public async Task<List<string>> GetCreatureTagsAsync(string creatureId)
        {
            var tags = new List<string>();

            var cmd = _conn.CreateCommand();
            cmd.CommandText = @"
                SELECT t.Name FROM Tags t
                INNER JOIN CreatureTags ct on t.Id = ct.TagId
                WHERE ct.CreatureId = @CreatureId
                ORDER BY t.Name";
            cmd.Parameters.AddWithValue("@CreatureId", creatureId);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                tags.Add(reader.GetString(0));
            }

            return tags;
        }

        public async Task<List<string>> GetAllTagsAsync()
        {
            var tags = new List<string>();

            var cmd = _conn.CreateCommand();
            cmd.CommandText = "SELECT Name FROM Tags ORDER BY Name";

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                tags.Add(reader.GetString(0));
            }

            return tags;
        }

        #endregion

        #region Search & Filter

        public async Task<List<Token>> SearchCreaturesAsync(
            string nameSearch = null,
            string type = null,
            string category = null,
            double? minCR = null,
            double? maxCR = null,
            int? minHP = null,
            int? maxHP = null,
            List<string> requiredTags = null,
            string sortBy = "Name",
            bool descending = false,
            int limit = 1000,
            int offset = 0)
        {
            var creatures = new List<Token>();
            var parameters = new List<SqliteParameter>();

            var sql = new StringBuilder();
            sql.Append("SELECT DISTINCT c.* FROM Creatures c ");

            if (requiredTags != null && requiredTags.Count > 0)
            {
                sql.Append(@"
                    INNER JOIN CreatureTags ct on c.Id = ct.CreatureId
                    INNER JOIN Tags t on ct.TagId = t.Id ");
            }

            sql.Append("WHERE 1=1 ");

            if (!string.IsNullOrWhiteSpace(nameSearch))
            {
                sql.Append("AND c.Name LIKE @NameSearch ");
                parameters.Add(new SqliteParameter("@NameSearch", $"%{nameSearch}%"));
            }

            if (!string.IsNullOrWhiteSpace(type) && type != "All")
            {
                sql.Append("AND c.Type = @Type ");
                parameters.Add(new SqliteParameter("@Type", type));
            }

            if (!string.IsNullOrWhiteSpace(category) && category != "All")
            {
                sql.Append("AND c.Category = @Category ");
                parameters.Add(new SqliteParameter("@Category", category));
            }

            if (minHP.HasValue)
            {
                sql.Append("AND c.MaxHP >= @MinHP ");
                parameters.Add(new SqliteParameter("@MinHP", minHP.Value));
            }
            if (maxHP.HasValue)
            {
                sql.Append("AND c.MaxHP <= @MaxHP ");
                parameters.Add(new SqliteParameter("@MaxHP", maxHP.Value));
            }

            if (requiredTags != null && requiredTags.Count > 0)
            {
                sql.Append("AND t.Name IN (");
                for (int i = 0; i < requiredTags.Count; i++)
                {
                    if (i > 0) sql.Append(", ");
                    sql.Append($"@Tag{i}");
                    parameters.Add(new SqliteParameter($"@Tag{i}", requiredTags[i]));
                }
                sql.Append(") ");
            }

            var validSortColumns = new[] { "Name", "Type", "ChallengeRating", "MaxHP", "ArmorClass", "Category" };
            if (!validSortColumns.Contains(sortBy))
                sortBy = "Name";

            sql.Append($"ORDER BY c.{sortBy} {(descending ? "DESC" : "ASC")} ");

            sql.Append("LIMIT @Limit OFFSET @Offset");
            parameters.Add(new SqliteParameter("@Limit", limit));
            parameters.Add(new SqliteParameter("@Offset", offset));

            var cmd = _conn.CreateCommand();
            cmd.CommandText = sql.ToString();
            foreach (var param in parameters)
            {
                cmd.Parameters.Add(param);
            }

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var creature = MapReaderToToken(reader);
                creatures.Add(creature);
            }

            foreach (var creature in creatures)
            {
                var id = creature.Id.ToString();
                creature.Actions = await GetActionsAsync(id, "Action");
                creature.BonusActions = await GetActionsAsync(id, "BonusAction");
                creature.Reactions = await GetActionsAsync(id, "Reaction");
                creature.LegendaryActions = await GetActionsAsync(id, "LegendaryAction");
                creature.Tags = await GetCreatureTagsAsync(id);
            }

            return creatures;
        }

        public async Task<List<string>> GetAllCategoriesAsync()
        {
            var categories = new List<string> { "All" };

            var cmd = _conn.CreateCommand();
            cmd.CommandText = "SELECT DISTINCT Category From Creatures WHERE Category != '' ORDER BY Category";

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                categories.Add(reader.GetString(0));
            }
            return categories;
        }

        public async Task<List<string>> GetAllTypesAsync()
        {
            var types = new List<string> { "All" };

            var cmd = _conn.CreateCommand();
            cmd.CommandText = "SELECT DISTINCT Type FROM Creatures WHERE Type != '' ORDER BY Type";

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                types.Add(reader.GetString(0));
            }
            return types;
        }

        public async Task<int> GetCreatureCountAsync()
        {
            var cmd = _conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM Creatures";
            return Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }

        #endregion

        #region JSON Import

        // Original Method
        /*public async Task<int> ImportFromJsonFileAsync(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"JSON file not found: {filePath}");

            var json = await File.ReadAllTextAsync(filePath);
            var category = Path.GetFileNameWithoutExtension(filePath);

            var options = new JsonSerializerOptions()
            {
                PropertyNameCaseInsensitive = true
            };

            var jsonCreatures = JsonSerializer.Deserialize<List<JsonCreatureDto>>(json, options);

            int importedCount = 0;

            foreach (var jc in jsonCreatures)
            {
                var token = ConvertJsonToToken(jc);
                await AddCreatureAsync(token, category, filePath);
                importedCount++;
            }
            return importedCount;
        }*/

        public async Task<int> ImportFromJsonFileAsync(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"JSON file not found: {filePath}");

            var category = Path.GetFileNameWithoutExtension(filePath);
            int importedCount = 0;

            try
            {
                // Read the file content
                string json;
                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                using (var reader = new StreamReader(stream, System.Text.Encoding.UTF8, detectEncodingFromByteOrderMarks: true))
                {
                    json = await reader.ReadToEndAsync();
                }

                // Remove BOM if present
                if (json.Length > 0 && json[0] == '\uFEFF')
                {
                    json = json.Substring(1);
                }

                json = json.Trim();

                if (string.IsNullOrEmpty(json))
                {
                    System.Diagnostics.Debug.WriteLine($"Empty file: {filePath}");
                    return 0;
                }

                // Use JsonDocument for more reliable parsing
                using (JsonDocument document = JsonDocument.Parse(json))
                {
                    JsonElement root = document.RootElement;

                    // Handle array of creatures
                    if (root.ValueKind == JsonValueKind.Array)
                    {
                        foreach (JsonElement creatureElement in root.EnumerateArray())
                        {
                            try
                            {
                                var token = ParseCreatureFromJsonElement(creatureElement);
                                if (token != null && !string.IsNullOrWhiteSpace(token.Name))
                                {
                                    await AddCreatureAsync(token, category, filePath);
                                    importedCount++;
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"Error parsing creature: {ex.Message}");
                            }
                        }
                    }
                    // Handle single creature object
                    else if (root.ValueKind == JsonValueKind.Object)
                    {
                        var token = ParseCreatureFromJsonElement(root);
                        if (token != null && !string.IsNullOrWhiteSpace(token.Name))
                        {
                            await AddCreatureAsync(token, category, filePath);
                            importedCount++;
                        }
                    }
                }
            }
            catch (JsonException ex)
            {
                System.Diagnostics.Debug.WriteLine($"JSON Parse Error in {filePath}: {ex.Message}");
                throw new Exception($"Failed to parse {Path.GetFileName(filePath)}: {ex.Message}");
            }

            return importedCount;
        }

        public async Task<int> ImportFromJsonFolderAsync(string folderPath, IProgress<string> progress = null)
        {
            if (!Directory.Exists(folderPath))
                throw new DirectoryNotFoundException($"Folder not found: {folderPath}");

            var jsonFiles = Directory.GetFiles(folderPath, "*.json");
            int totalImported = 0;

            foreach (var file in jsonFiles)
            {
                progress?.Report($"Importing {Path.GetFileName(file)}...");
                var count = await ImportFromJsonFileAsync(file);
                totalImported += count;
                progress?.Report($"Imported {count} creatures from {Path.GetFileName(file)}");
            }
            return totalImported;
        }

        private Token ConvertJsonToToken(JsonCreatureDto jc)
        {
            return new Token
            {
                Id = !string.IsNullOrEmpty(jc.Id) ? Guid.Parse(jc.Id) : Guid.NewGuid(),
                Name = jc.Name ?? "Unknown",
                Size = jc.Size ?? "",
                Type = jc.Type ?? "",
                Alignment = jc.Alignment ?? "",
                ChallengeRating = jc.ChallengeRating ?? "",
                ArmorClass = jc.ArmorClass,
                MaxHP = jc.MaxHP,
                HP = jc.MaxHP,
                HitDice = jc.HitDice ?? "",
                InitiativeModifier = jc.InitiativeMod,
                Speed = jc.Speed ?? "",
                Str = jc.Str,
                Dex = jc.Dex,
                Con = jc.Con,
                Int = jc.Int,
                Wis = jc.Wis,
                Cha = jc.Cha,
                Skills = jc.Skills ?? new List<string>(),
                Senses = jc.Senses ?? "",
                Languages = jc.Languages ?? "",
                Immunities = jc.Immunities ?? "",
                Resistances = jc.Resistances ?? "",
                Vulnerabilities = jc.Vulnerabilities ?? "",
                Traits = jc.Traits ?? "",
                Notes = jc.Notes ?? "",
                Actions = jc.Actions?.Select(a => new Models.Action
                {
                    Name = a.Name ?? "",
                    AttackBonus = a.AttackBonus,
                    DamageExpression = a.DamageExpression ?? "",
                    Range = a.Range ?? "",
                    Description = a.Description ?? ""
                }).ToList() ?? new List<Models.Action>(),
                BonusActions = jc.BonusActions?.Select(a => new Models.Action
                {
                    Name = a.Name ?? "",
                    AttackBonus = a.AttackBonus,
                    DamageExpression = a.DamageExpression ?? "",
                    Range = a.Range ?? "",
                    Description = a.Description ?? ""
                }).ToList() ?? new List<Models.Action>(),
                Reactions = jc.Reactions?.Select(a => new Models.Action
                {
                    Name = a.Name ?? "",
                    AttackBonus = a.AttackBonus,
                    DamageExpression = a.DamageExpression ?? "",
                    Range = a.Range ?? "",
                    Description = a.Description ?? ""
                }).ToList() ?? new List<Models.Action>(),
                LegendaryActions = jc.LegendaryActions?.Select(a => new Models.Action
                {
                    Name = a.Name ?? "",
                    AttackBonus = a.AttackBonus,
                    DamageExpression = a.DamageExpression ?? "",
                    Range = a.Range ?? "",
                    Description = a.Description ?? ""
                }).ToList() ?? new List<Models.Action>(),
                Tags = new List<string>() // Tags start empty, users add them
            };
        }

        #endregion

        #region Helpers

        private Token MapReaderToToken(SqliteDataReader reader)
        {
            return new Token
            {
                Id = Guid.Parse(reader.GetString(reader.GetOrdinal("Id"))),
                Name = reader.GetString(reader.GetOrdinal("Name")),
                Size = reader.IsDBNull(reader.GetOrdinal("Size")) ? "" : reader.GetString(reader.GetOrdinal("Size")),
                Type = reader.IsDBNull(reader.GetOrdinal("Type")) ? "" : reader.GetString(reader.GetOrdinal("Type")),
                Alignment = reader.IsDBNull(reader.GetOrdinal("Alignment")) ? "" : reader.GetString(reader.GetOrdinal("Alignment")),
                ChallengeRating = reader.IsDBNull(reader.GetOrdinal("ChallengeRating")) ? "" : reader.GetString(reader.GetOrdinal("ChallengeRating")),
                ArmorClass = reader.GetInt32(reader.GetOrdinal("ArmorClass")),
                MaxHP = reader.GetInt32(reader.GetOrdinal("MaxHP")),
                HP = reader.GetInt32(reader.GetOrdinal("MaxHP")),
                HitDice = reader.IsDBNull(reader.GetOrdinal("HitDice")) ? "" : reader.GetString(reader.GetOrdinal("HitDice")),
                InitiativeModifier = reader.GetInt32(reader.GetOrdinal("InitiativeModifier")),
                Speed = reader.IsDBNull(reader.GetOrdinal("Speed")) ? "" : reader.GetString(reader.GetOrdinal("Speed")),
                Str = reader.GetInt32(reader.GetOrdinal("Str")),
                Dex = reader.GetInt32(reader.GetOrdinal("Dex")),
                Con = reader.GetInt32(reader.GetOrdinal("Con")),
                Int = reader.GetInt32(reader.GetOrdinal("Int")),
                Wis = reader.GetInt32(reader.GetOrdinal("Wis")),
                Cha = reader.GetInt32(reader.GetOrdinal("Cha")),
                Skills = ParseSkillsList(reader.IsDBNull(reader.GetOrdinal("Skills")) ? "" : reader.GetString(reader.GetOrdinal("Skills"))),
                Senses = reader.IsDBNull(reader.GetOrdinal("Senses")) ? "" : reader.GetString(reader.GetOrdinal("Senses")),
                Languages = reader.IsDBNull(reader.GetOrdinal("Languages")) ? "" : reader.GetString(reader.GetOrdinal("Languages")),
                Immunities = reader.IsDBNull(reader.GetOrdinal("Immunities")) ? "" : reader.GetString(reader.GetOrdinal("Immunities")),
                Resistances = reader.IsDBNull(reader.GetOrdinal("Resistances")) ? "" : reader.GetString(reader.GetOrdinal("Resistances")),
                Vulnerabilities = reader.IsDBNull(reader.GetOrdinal("Vulnerabilities")) ? "" : reader.GetString(reader.GetOrdinal("Vulnerabilities")),
                Traits = reader.IsDBNull(reader.GetOrdinal("Traits")) ? "" : reader.GetString(reader.GetOrdinal("Traits")),
                Notes = reader.IsDBNull(reader.GetOrdinal("Notes")) ? "" : reader.GetString(reader.GetOrdinal("Notes")),
                IconPath = reader.IsDBNull(reader.GetOrdinal("IconPath")) ? "" : reader.GetString(reader.GetOrdinal("IconPath")),
                SizeInSquares = reader.GetInt32(reader.GetOrdinal("SizeInSquares"))
            };
        }

        private List<string> ParseSkillsList(string skills)
        {
            if (string.IsNullOrWhiteSpace(skills))
                return new List<string>();

            return skills.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .ToList();
        }

        private Token ParseCreatureFromJsonElement(JsonElement element)
        {
            var token = new Token();

            // Parse each property safely
            token.Id = GetGuidProperty(element, "Id") ?? Guid.NewGuid();
            token.Name = GetStringProperty(element, "Name") ?? "Unknown";
            token.Size = GetStringProperty(element, "Size") ?? "";
            token.Type = GetStringProperty(element, "Type") ?? "";
            token.Alignment = GetStringProperty(element, "Alignment") ?? "";
            token.ChallengeRating = GetStringProperty(element, "ChallengeRating") ?? "";
            token.ArmorClass = GetIntProperty(element, "ArmorClass");
            token.MaxHP = GetIntProperty(element, "MaxHP");
            token.HP = token.MaxHP;
            token.HitDice = GetStringProperty(element, "HitDice") ?? "";
            token.InitiativeModifier = GetIntProperty(element, "InitiativeMod");
            token.Speed = GetStringProperty(element, "Speed") ?? "";
            token.Str = GetIntProperty(element, "Str", 10);
            token.Dex = GetIntProperty(element, "Dex", 10);
            token.Con = GetIntProperty(element, "Con", 10);
            token.Int = GetIntProperty(element, "Int", 10);
            token.Wis = GetIntProperty(element, "Wis", 10);
            token.Cha = GetIntProperty(element, "Cha", 10);
            token.Senses = GetStringProperty(element, "Senses") ?? "";
            token.Languages = GetStringProperty(element, "Languages") ?? "";
            token.Immunities = GetStringProperty(element, "Immunities") ?? "";
            token.Resistances = GetStringProperty(element, "Resistances") ?? "";
            token.Vulnerabilities = GetStringProperty(element, "Vulnerabilities") ?? "";
            token.Traits = GetStringProperty(element, "Traits") ?? "";
            token.Notes = GetStringProperty(element, "Notes") ?? "";
            token.IconPath = GetStringProperty(element, "IconPath") ?? "";
            token.SizeInSquares = GetIntProperty(element, "SizeInSquares", 1);

            // Parse Skills array
            token.Skills = new List<string>();
            if (element.TryGetProperty("Skills", out JsonElement skillsElement) && skillsElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var skill in skillsElement.EnumerateArray())
                {
                    if (skill.ValueKind == JsonValueKind.String)
                    {
                        token.Skills.Add(skill.GetString());
                    }
                }
            }

            // Parse Actions
            token.Actions = ParseActionsArray(element, "Actions");
            token.BonusActions = ParseActionsArray(element, "BonusActions");
            token.Reactions = ParseActionsArray(element, "Reactions");
            token.LegendaryActions = ParseActionsArray(element, "LegendaryActions");

            // Initialize empty tags list (your JSON doesn't have tags, users add them later)
            token.Tags = new List<string>();

            return token;
        }

        private string GetStringProperty(JsonElement element, string propertyName)
        {
            if (element.TryGetProperty(propertyName, out JsonElement prop))
            {
                if (prop.ValueKind == JsonValueKind.String)
                    return prop.GetString();
                if (prop.ValueKind == JsonValueKind.Null)
                    return null;
            }
            return null;
        }

        private int GetIntProperty(JsonElement element, string propertyName, int defaultValue = 0)
        {
            if (element.TryGetProperty(propertyName, out JsonElement prop))
            {
                if (prop.ValueKind == JsonValueKind.Number)
                    return prop.GetInt32();
                if (prop.ValueKind == JsonValueKind.String && int.TryParse(prop.GetString(), out int val))
                    return val;
            }
            return defaultValue;
        }

        private Guid? GetGuidProperty(JsonElement element, string propertyName)
        {
            if (element.TryGetProperty(propertyName, out JsonElement prop))
            {
                if (prop.ValueKind == JsonValueKind.String)
                {
                    var str = prop.GetString();
                    if (Guid.TryParse(str, out Guid guid))
                        return guid;
                }
            }
            return null;
        }

        private List<Models.Action> ParseActionsArray(JsonElement element, string propertyName)
        {
            var actions = new List<Models.Action>();

            if (element.TryGetProperty(propertyName, out JsonElement actionsElement) &&
                actionsElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var actionElement in actionsElement.EnumerateArray())
                {
                    var action = new Models.Action
                    {
                        Name = GetStringProperty(actionElement, "Name") ?? "",
                        AttackBonus = GetIntProperty(actionElement, "AttackBonus"),
                        DamageExpression = GetStringProperty(actionElement, "DamageExpression") ?? "",
                        Range = GetStringProperty(actionElement, "Range") ?? "",
                        Description = GetStringProperty(actionElement, "Description") ?? ""
                    };
                    actions.Add(action);
                }
            }

            return actions;
        }

        public void Dispose()
        {
            _conn?.Close();
            _conn?.Dispose();
        }

        #endregion
    }

    public class JsonCreatureDto
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Size { get; set; }
        public string Type { get; set; }
        public string Alignment { get; set; }
        public string ChallengeRating { get; set; }
        public int ArmorClass { get; set; }
        public int MaxHP { get; set; }
        public string HitDice { get; set; }
        public int InitiativeMod { get; set; }
        public string Speed { get; set; }
        public int Str { get; set; }
        public int Dex { get; set; }
        public int Con { get; set; }
        public int Int { get; set; }
        public int Wis { get; set; }
        public int Cha { get; set; }
        public List<string> Skills { get; set; }
        public string Senses { get; set; }
        public string Languages { get; set; }
        public string Immunities { get; set; }
        public string Resistances { get; set; }
        public string Vulnerabilities { get; set; }
        public string Traits { get; set; }
        public string Notes { get; set; }
        public List<JsonActionDto> Actions { get; set; }
        public List<JsonActionDto> BonusActions { get; set; }
        public List<JsonActionDto> Reactions { get; set; }
        public List<JsonActionDto> LegendaryActions { get; set; }
    }

    public class JsonActionDto
    {
        public string Name { get; set; }
        public int AttackBonus { get; set; }
        public string DamageExpression { get; set; }
        public string Range { get; set; }
        public string Description { get; set; }
    }
}

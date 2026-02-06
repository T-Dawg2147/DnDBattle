using DnDBattle.Models;
using DnDBattle.Views;
using DnDBattle.Views.Combat;
using DnDBattle.Views.Creatures;
using DnDBattle.Views.Dice;
using DnDBattle.Views.Effects;
using DnDBattle.Views.Encounters;
using DnDBattle.Views.Features;
using DnDBattle.Views.Multiplayer;
using DnDBattle.Views.Settings;
using DnDBattle.Views.Spells;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.DirectoryServices;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using DnDBattle.Services;
using DnDBattle.Services.Combat;
using DnDBattle.Services.Dice;
using DnDBattle.Services.Effects;
using DnDBattle.Services.Encounters;
using DnDBattle.Services.Grid;
using DnDBattle.Services.Networking;
using DnDBattle.Services.Persistence;
using DnDBattle.Services.TileService;
using DnDBattle.Services.UI;
using DnDBattle.Services.Vision;
using DnDBattle.Views.Editors;
using DnDBattle.Views.TileMap;
using DnDBattle.Models.Combat;
using DnDBattle.Models.Creatures;
using DnDBattle.Models.Effects;
using DnDBattle.Models.Encounters;
using DnDBattle.Models.Environment;
using DnDBattle.Models.Networking;
using DnDBattle.Models.Spells;
using DnDBattle.Models.Tiles;

namespace DnDBattle.Services.Creatures
{
    public class CreatureDatabaseService : IDisposable
    {
        private readonly string _dbPath;
        private SqliteConnection _conn;
        private bool _isInitialized = false;
        private readonly SemaphoreSlim _initLock = new SemaphoreSlim(1, 1);

        public CreatureDatabaseService(string dbPath = "CreatureBank.db")
        {
            _dbPath = dbPath;
        }

        #region Database Initialization

        public async Task EnsureInitializedAsync()
        {
            if (_isInitialized) return;

            await _initLock.WaitAsync();
            try
            {
                if (_isInitialized) return;

                await InitializeDatabaseAsync();
                _isInitialized = true;
            }
            finally
            {
                _initLock.Release();
            }
        }

        private async Task InitializeDatabaseAsync()
        {
            _conn = new SqliteConnection($"Data Source={_dbPath};Cache=Shared;Mode=ReadWriteCreate");
            await _conn.OpenAsync();

            // Enable WAL mode for better concurrent read performance
            using (var walCmd = _conn.CreateCommand())
            {
                walCmd.CommandText = "PRAGMA journal_mode=WAL; PRAGMA synchronous=NORMAL; PRAGMA cache_size=10000;";
                await walCmd.ExecuteNonQueryAsync();
            }

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
                    Skills TEXT,
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
                    DateAdded TEXT,
                    IsFavorite INTEGER DEFAULT 0
                );

                -- Actions table
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

                -- Tags table
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
                CREATE INDEX IF NOT EXISTS idx_creatures_favorite ON Creatures(IsFavorite);
                CREATE INDEX IF NOT EXISTS idx_tags_name ON Tags(Name);
            ";
            await createTablesCmd.ExecuteNonQueryAsync();

            // Add IsFavorite column if it doesn't exist (for existing databases)
            try
            {
                using var alterCmd = _conn.CreateCommand();
                alterCmd.CommandText = "ALTER TABLE Creatures ADD COLUMN IsFavorite INTEGER DEFAULT 0";
                await alterCmd.ExecuteNonQueryAsync();
            }
            catch { /* Column already exists */ }
        }

        private async Task EnsureConnectionAsync()
        {
            await EnsureInitializedAsync();
            if (_conn.State != System.Data.ConnectionState.Open)
                await _conn.OpenAsync();
        }

        public async Task<List<CreatureSummary>> GetCreatureSummariesAsync(string nameSearch = null, string type = null, string category = null, int limit = 500, int offset = 0)
        {
            var summaries = new List<CreatureSummary>();

            var sql = new StringBuilder();
            sql.Append(@"SELECT Id, Name, Size, Type, ChallengeRating, ArmorClass, MaxHP, IconPath
                        FROM Creatures WHERE 1=1 ");

            var parameters = new List<SqliteParameter>();

            if (!string.IsNullOrWhiteSpace(nameSearch))
            {
                sql.Append("AND Name LIKE @NameSearch ");
                parameters.Add(new SqliteParameter("@NameSearch", $"%{nameSearch}%"));
            }

            if (!string.IsNullOrWhiteSpace(type) && type != "All")
            {
                sql.Append("AND Type = @Type ");
                parameters.Add(new SqliteParameter("Type", type));
            }

            if (!string.IsNullOrWhiteSpace(category) && category != "All")
            {
                sql.Append("AND Category = @Category ");
                parameters.Add(new SqliteParameter("@Category", category));
            }

            sql.Append("ORDER BY Name LIMIT @Limit OFFSET @Offset");
            parameters.Add(new SqliteParameter("@Limit", limit));
            parameters.Add(new SqliteParameter("@Offset", offset));

            var cmd = _conn.CreateCommand();
            cmd.CommandText = sql.ToString();
            foreach (var p in parameters)
                cmd.Parameters.Add(p);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                summaries.Add(new CreatureSummary()
                {
                    Id = reader.GetString(0),
                    Name = reader.IsDBNull(1) ? "" : reader.GetString(1),
                    Size = reader.IsDBNull(2) ? "" : reader.GetString(2),
                    Type = reader.IsDBNull(3) ? "" : reader.GetString(3),
                    ChallengeRating = reader.IsDBNull(4) ? "" : reader.GetString(4),
                    ArmorClass = reader.IsDBNull(5) ? 0 : reader.GetInt32(5),
                    MaxHP = reader.IsDBNull(6) ? 0 : reader.GetInt32(6),
                    IconPath = reader.IsDBNull(7) ? "" : reader.GetString(7)
                });
            }
            return summaries;
        }

        #endregion

        #region Creature CRUD Operations

        public async Task<int> AddCreatureAsync(Token creature, string category = null, string sourceFile = null)
        {
            await EnsureConnectionAsync();

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
                cmd.Parameters.AddWithValue("@ArmorClass", creature.ArmorClass);
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
                cmd.Parameters.AddWithValue("@Senses", creature.Senses ?? "");
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
                cmd.Parameters.AddWithValue("@DateAdded", DateTime.Now.ToString("O"));

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

        private async Task InsertActionsAsync(string creatureId, string actionType, List<Models.Combat.Action> actions)
        {
            if (actions == null || actions.Count == 0) return;
            
            foreach (var action in actions)
            {
                var cmd = _conn.CreateCommand();
                cmd.CommandText = @"
                    INSERT INTO CreatureActions (CreatureId, ActionType, Name, AttackBonus, DamageExpression, Range, Description)
                    VALUES (@CreatureId, @ActionType, @Name, @AttackBonus, @DamageExpression, @Range, @Description)";

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
            await EnsureConnectionAsync();

            var cmd = _conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM Creatures WHERE Id = @Id";
            cmd.Parameters.AddWithValue("@Id", id);

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var creature = MapReaderToToken(reader);
                creature.Actions = await GetActionsAsync(id, "Action");
                creature.BonusActions = await GetActionsAsync(id, "BonusAction");
                creature.Reactions = await GetActionsAsync(id, "Reaction");
                creature.LegendaryActions = await GetActionsAsync(id, "LegendaryAction");  
                creature.Tags = await GetCreatureTagsAsync(id);
                return creature;
            }

            return null;
        }

        public async Task<List<Models.Combat.Action>> GetActionsAsync(string creatureId, string actionType = null)
        {
            await EnsureConnectionAsync();

            var actions = new List<Models.Combat.Action>();
            var cmd = _conn.CreateCommand();
            cmd.CommandText = @"
                SELECT Name, AttackBonus, DamageExpression, Range, Description
                FROM CreatureActions
                WHERE CreatureId = @CreatureId ";
            if (!string.IsNullOrWhiteSpace(actionType))
            {
                cmd.CommandText += "AND ActionType = @ActionType";
                cmd.Parameters.AddWithValue("@ActionType", actionType);
            }
            cmd.Parameters.AddWithValue("@CreatureId", creatureId);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                actions.Add(new Models.Combat.Action()
                {
                    Name = reader.GetString(0),
                    AttackBonus = reader.GetInt32(1),
                    DamageExpression = reader.GetString(2),
                    Range = reader.GetString(3),
                    Description = reader.GetString(4),
                    Type = actionType
                });
            }

            return actions;
        }

        public async Task ClearAllCreaturesAsync()
        {
            await EnsureConnectionAsync();

            using var transaction = _conn.BeginTransaction();

            try
            {
                var deleteTagsCmd = _conn.CreateCommand();
                deleteTagsCmd.CommandText = "DELETE FROM CreatureTags";
                await deleteTagsCmd.ExecuteNonQueryAsync();

                var deleteActionsCmd = _conn.CreateCommand();
                deleteActionsCmd.CommandText = "DELETE FROM CreatureActions";
                await deleteActionsCmd.ExecuteNonQueryAsync();

                var deleteCreaturesCmd = _conn.CreateCommand();
                deleteCreaturesCmd.CommandText = "DELETE FROM Creatures";
                await deleteCreaturesCmd.ExecuteNonQueryAsync();

                var deleteOrphanTagsCmd = _conn.CreateCommand();
                deleteOrphanTagsCmd.CommandText = @"
                    DELETE FROM Tags WHERE Id NOT IN (SELECT DISTINCT TagId FROM CreatureTags)";
                await deleteOrphanTagsCmd.ExecuteNonQueryAsync();

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        #endregion

        #region Category Management

        /// <summary>
        /// Creates the categories table if it doesn't exist
        /// </summary>
        public async Task EnsureCategoryTableExistsAsync()
        {
            await EnsureConnectionAsync();

            var cmd = _conn.CreateCommand();
            cmd.CommandText = @"
        CREATE TABLE IF NOT EXISTS Categories (
            Id TEXT PRIMARY KEY,
            Name TEXT NOT NULL,
            ParentId TEXT,
            SortOrder INTEGER DEFAULT 0,
            Icon TEXT,
            IsSystem INTEGER DEFAULT 0,
            CreatedAt TEXT DEFAULT CURRENT_TIMESTAMP
        );
        
        CREATE TABLE IF NOT EXISTS CreatureCategories (
            CreatureId TEXT NOT NULL,
            CategoryId TEXT NOT NULL,
            PRIMARY KEY (CreatureId, CategoryId),
            FOREIGN KEY (CreatureId) REFERENCES Creatures(Id),
            FOREIGN KEY (CategoryId) REFERENCES Categories(Id)
        );
        
        CREATE TABLE IF NOT EXISTS Favorites (
            CreatureId TEXT PRIMARY KEY,
            AddedAt TEXT DEFAULT CURRENT_TIMESTAMP,
            FOREIGN KEY (CreatureId) REFERENCES Creatures(Id)
        );
    ";
            await cmd.ExecuteNonQueryAsync();

            // Ensure default categories exist
            await EnsureDefaultCategoriesAsync();
        }

        private async Task EnsureDefaultCategoriesAsync()
        {
            var defaultCategories = new (string id, string name, string parentId, int sortOrder, string icon, bool isSystem)[]
            {
                ("dnd5e-srd", "D&D 5e SRD", null, 0, "📚", true),
                ("custom", "Custom Creatures", null, 1, "✨", true),
                ("npcs", "NPCs", null, 2, "👤", true),
                ("player-characters", "Player Characters", null, 3, "⚔️", true),
                ("favorites", "Favorites", null, -1, "⭐", true)
            };

            await EnsureConnectionAsync();

            foreach (var category in defaultCategories)
            {
                var checkCmd = _conn.CreateCommand();
                checkCmd.CommandText = "SELECT COUNT(*) FROM Categories WHERE Id = @Id";
                checkCmd.Parameters.AddWithValue("@Id", category.id);
                var count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());

                if (count == 0)
                {
                    var insertCmd = _conn.CreateCommand();
                    insertCmd.CommandText = @"
                        INSERT INTO Categories (Id, Name, ParentId, SortOrder, Icon, IsSystem)
                        VALUES (@Id, @Name, @ParentId, @SortOrder, @Icon, @IsSystem)";
                    insertCmd.Parameters.AddWithValue("@Id", category.id);
                    insertCmd.Parameters.AddWithValue("@Name", category.name);
                    insertCmd.Parameters.AddWithValue("@ParentId", category.parentId ?? (object)DBNull.Value);
                    insertCmd.Parameters.AddWithValue("@SortOrder", category.sortOrder);
                    insertCmd.Parameters.AddWithValue("@Icon", category.icon);
                    insertCmd.Parameters.AddWithValue("@IsSystem", category.isSystem ? 1 : 0);
                    await insertCmd.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task<List<CreatureCategory>> GetCategoriesAsync()
        {
            await EnsureConnectionAsync();

            var categories = new List<CreatureCategory>();

            try
            {
                var cmd = _conn.CreateCommand();
                cmd.CommandText = @"
            SELECT Id, Name, ParentId, SortOrder, Icon, IsSystem 
            FROM Categories 
            ORDER BY SortOrder, Name";

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    categories.Add(new CreatureCategory
                    {
                        Id = reader.IsDBNull(0) ? "" : reader.GetString(0),
                        Name = reader.IsDBNull(1) ? "" : reader.GetString(1),
                        ParentId = reader.IsDBNull(2) ? null : reader.GetString(2),
                        SortOrder = reader.IsDBNull(3) ? 0 : reader.GetInt32(3),
                        Icon = reader.IsDBNull(4) ? "📁" : reader.GetString(4),
                        IsSystem = reader.IsDBNull(5) ? false : reader.GetInt32(5) == 1,
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting categories: {ex.Message}");
            }

            return categories;
        }

        public async Task<bool> AddCategoryAsync(string id, string name, string icon = "📁", string parentId = null, int sortOrder = 0)
        {
            await EnsureConnectionAsync();

            try
            {
                var cmd = _conn.CreateCommand();
                cmd.CommandText = @"
            INSERT INTO Categories (Id, Name, ParentId, SortOrder, Icon, IsSystem, CreatedAt)
            VALUES (@Id, @Name, @ParentId, @SortOrder, @Icon, 0, @CreatedAt)";

                cmd.Parameters.AddWithValue("@Id", id);
                cmd.Parameters.AddWithValue("@Name", name);
                cmd.Parameters.AddWithValue("@ParentId", (object)parentId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@SortOrder", sortOrder);
                cmd.Parameters.AddWithValue("@Icon", icon);
                cmd.Parameters.AddWithValue("@CreatedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                await cmd.ExecuteNonQueryAsync();
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error adding category: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Deletes a category (only if not a system category)
        /// </summary>
        public async Task<bool> DeleteCategoryAsync(string categoryId)
        {
            await EnsureConnectionAsync();

            try
            {
                // Check if it's a system category
                var checkCmd = _conn.CreateCommand();
                checkCmd.CommandText = "SELECT IsSystem FROM Categories WHERE Id = @Id";
                checkCmd.Parameters.AddWithValue("@Id", categoryId);

                var result = await checkCmd.ExecuteScalarAsync();
                if (result != null && Convert.ToInt32(result) == 1)
                {
                    return false; // Can't delete system categories
                }

                // Move creatures in this category to "custom"
                var moveCmd = _conn.CreateCommand();
                moveCmd.CommandText = "UPDATE Creatures SET Category = 'custom' WHERE Category = @Id";
                moveCmd.Parameters.AddWithValue("@Id", categoryId);
                await moveCmd.ExecuteNonQueryAsync();

                // Delete the category
                var deleteCmd = _conn.CreateCommand();
                deleteCmd.CommandText = "DELETE FROM Categories WHERE Id = @Id AND IsSystem = 0";
                deleteCmd.Parameters.AddWithValue("@Id", categoryId);
                await deleteCmd.ExecuteNonQueryAsync();

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error deleting category: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> RenameCategoryAsync(string categoryId, string newName)
        {
            await EnsureConnectionAsync();

            try
            {
                var cmd = _conn.CreateCommand();
                cmd.CommandText = "UPDATE Categories SET Name = @Name WHERE Id = @Id";
                cmd.Parameters.AddWithValue("@Name", newName);
                cmd.Parameters.AddWithValue("@Id", categoryId);

                await cmd.ExecuteNonQueryAsync();
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error renaming category: {ex.Message}");
                return false;
            }
        }

        public async Task AssignCreatureToCategoryAsync(string creatureId, string categoryId)
        {
            await EnsureConnectionAsync();

            var cmd = _conn.CreateCommand();
            cmd.CommandText = @"
        INSERT OR IGNORE INTO CreatureCategories (CreatureId, CategoryId)
        VALUES (@CreatureId, @CategoryId)";
            cmd.Parameters.AddWithValue("@CreatureId", creatureId);
            cmd.Parameters.AddWithValue("@CategoryId", categoryId);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task RemoveCreatureFromCategoryAsync(string creatureId, string categoryId)
        {
            await EnsureConnectionAsync();

            var cmd = _conn.CreateCommand();
            cmd.CommandText = "DELETE FROM CreatureCategories WHERE CreatureId = @CreatureId AND CategoryId = @CategoryId";
            cmd.Parameters.AddWithValue("@CreatureId", creatureId);
            cmd.Parameters.AddWithValue("@CategoryId", categoryId);
            await cmd.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Gets creatures by category ID - checks the Category column in Creatures table
        /// </summary>
        public async Task<List<Token>> GetCreaturesByCategoryAsync(string categoryId, int limit = 1000)
        {
            await EnsureConnectionAsync();

            System.Diagnostics.Debug.WriteLine($"GetCreaturesByCategoryAsync called with categoryId: {categoryId}");

            // Special handling for "favorites"
            if (categoryId?.ToLower() == "favorites")
            {
                var favs = await GetFavoritesAsync();
                var favoriteCreatures = new List<Token>();
                foreach (var f in favs)
                {
                    var creature = await GetCreatureByIdAsync(f.Id);
                    if (creature != null)
                        favoriteCreatures.Add(creature);
                }
                return favoriteCreatures;
            }

            // Special handling for "all" or main SRD category
            if (string.IsNullOrEmpty(categoryId) ||
                categoryId.ToLower() == "all" ||
                categoryId.ToLower() == "dnd5e-srd")
            {
                return await GetAllCreaturesAsync(limit);
            }

            var creatures = new List<Token>();

            try
            {
                var cmd = _conn.CreateCommand();
                cmd.CommandText = @"
            SELECT * FROM Creatures 
            WHERE Category = @Category OR Category IS NULL OR Category = ''
            ORDER BY Name 
            LIMIT @Limit";
                cmd.Parameters.AddWithValue("@Category", categoryId);
                cmd.Parameters.AddWithValue("@Limit", limit);

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    creatures.Add(ReadCreatureFromReader(reader));
                }

                System.Diagnostics.Debug.WriteLine($"GetCreaturesByCategoryAsync: Found {creatures.Count} creatures for category {categoryId}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting creatures by category: {ex.Message}");
            }

            return creatures;
        }

        /// <summary>
        /// Gets the count of creatures in a specific category
        /// </summary>
        public async Task<int> GetCreatureCountByCategoryAsync(string categoryId)
        {
            await EnsureConnectionAsync();

            try
            {
                var cmd = _conn.CreateCommand();

                if (categoryId?.ToLower() == "favorites")
                {
                    cmd.CommandText = "SELECT COUNT(*) FROM Creatures WHERE IsFavorite = 1";
                }
                else if (string.IsNullOrEmpty(categoryId) ||
                         categoryId.ToLower() == "all" ||
                         categoryId.ToLower() == "dnd5e-srd")
                {
                    cmd.CommandText = "SELECT COUNT(*) FROM Creatures";
                }
                else
                {
                    cmd.CommandText = "SELECT COUNT(*) FROM Creatures WHERE Category = @Category";
                    cmd.Parameters.AddWithValue("@Category", categoryId);
                }

                var result = await cmd.ExecuteScalarAsync();
                return Convert.ToInt32(result);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting creature count: {ex.Message}");
                return 0;
            }
        }

        #endregion

        #region Favorites

        public async Task<bool> IsCreatureFavoriteAsync(string creatureId)
        {
            await EnsureCategoryTableExistsAsync();

            var cmd = _conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM Favorites WHERE CreatureId = @CreatureId";
            cmd.Parameters.AddWithValue("@CreatureId", creatureId);
            return Convert.ToInt32(await cmd.ExecuteScalarAsync()) > 0;
        }

        public async Task ToggleFavoriteAsync(string creatureId)
        {
            await EnsureConnectionAsync();

            var cmd = _conn.CreateCommand();
            cmd.CommandText = "UPDATE Creatures SET IsFavorite = NOT IsFavorite WHERE Id = @Id";
            cmd.Parameters.AddWithValue("@Id", creatureId);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task SetFavoriteAsync(string creatureId, bool isFavorite)
        {
            await EnsureInitializedAsync();

            var cmd = _conn.CreateCommand();
            cmd.CommandText = "UPDATE Creatures SET IsFavorite = @IsFavorite WHERE Id = @Id";
            cmd.Parameters.AddWithValue("@Id", creatureId);
            cmd.Parameters.AddWithValue("@IsVaorite", isFavorite);
            await cmd.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Get all favorite creatures (lightweight)
        /// </summary>
        public async Task<List<CreatureSummary>> GetFavoritesAsync()
        {
            await EnsureConnectionAsync();

            var summaries = new List<CreatureSummary>();

            var cmd = _conn.CreateCommand();
            cmd.CommandText = @"SELECT Id, Name, Size, Type, ChallengeRating, ArmorClass, MaxHP, IconPath 
                        FROM Creatures WHERE IsFavorite = 1 ORDER BY Name";

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                summaries.Add(new CreatureSummary
                {
                    Id = reader.GetString(0),
                    Name = reader.IsDBNull(1) ? "" : reader.GetString(1),
                    Size = reader.IsDBNull(2) ? "" : reader.GetString(2),
                    Type = reader.IsDBNull(3) ? "" : reader.GetString(3),
                    ChallengeRating = reader.IsDBNull(4) ? "" : reader.GetString(4),
                    ArmorClass = reader.IsDBNull(5) ? 0 : reader.GetInt32(5),
                    MaxHP = reader.IsDBNull(6) ? 0 : reader.GetInt32(6),
                    IconPath = reader.IsDBNull(7) ? "" : reader.GetString(7)
                });
            }

            return summaries;
        }

        public async Task<bool> IsFavoriteAsync(string creatureId)
        {
            await EnsureConnectionAsync();

            var cmd = _conn.CreateCommand();
            cmd.CommandText = "SELECT IsFavorite FROM Creatures WHERE Id = @Id";
            cmd.Parameters.AddWithValue("@Id", creatureId);

            var result = await cmd.ExecuteScalarAsync();
            return result != null && Convert.ToInt32(result) == 1;
        }

        #endregion

        #region Single Creature Operations

        public async Task<Token> AddCustomCreatureAsync(Token creature, string categoryId = "custom")
        {
            await EnsureConnectionAsync();

            creature.Id = Guid.NewGuid();
            await AddCreatureAsync(creature, categoryId, "custom");
            await AssignCreatureToCategoryAsync(creature.Id.ToString(), categoryId);
            return creature;
        }

        public async Task UpdateCreatureAsync(Token creature)
        {
            await EnsureConnectionAsync();

            var cmd = _conn.CreateCommand();
            cmd.CommandText = @"
            UPDATE Creatures SET
                Name = @Name,
                Size = @Size,
                Type = @Type,
                Alignment = @Alignment,
                ChallengeRating = @ChallengeRating,
                ArmorClass = @ArmorClass,
                MaxHP = @MaxHP,
                HitDice = @HitDice,
                InitiativeModifier = @InitiativeModifier,
                Speed = @Speed,
                Str = @Str,
                Dex = @Dex,
                Con = @Con,
                Int = @Int,
                Wis = @Wis,
                Cha = @Cha,
                Skills = @Skills,
                Senses = @Senses,
                Languages = @Languages,
                Immunities = @Immunities,
                Resistances = @Resistances,
                Vulnerabilities = @Vulnerabilities,
                Traits = @Traits,
                Notes = @Notes,
                IconPath = @IconPath,
                SizeInSquares = @SizeInSquares
            WHERE Id = @Id;
            ";

            cmd.Parameters.AddWithValue("@Id", creature.Id.ToString());
            cmd.Parameters.AddWithValue("@Name", creature.Name ?? "");
            cmd.Parameters.AddWithValue("@Size", creature.Size ?? "");
            cmd.Parameters.AddWithValue("@Type", creature.Type ?? "");
            cmd.Parameters.AddWithValue("@Alignment", creature.Alignment ?? "");
            cmd.Parameters.AddWithValue("@ChallengeRating", creature.ChallengeRating ?? "");
            cmd.Parameters.AddWithValue("@ArmorClass", creature.ArmorClass);
            cmd.Parameters.AddWithValue("@MaxHP", creature.MaxHP);
            cmd.Parameters.AddWithValue("@HitDice", creature.HitDice ?? "");
            cmd.Parameters.AddWithValue("@InitiativeModifier", creature.InitiativeModifier);
            cmd.Parameters.AddWithValue("@Speed", creature.Speed ?? "");
            cmd.Parameters.AddWithValue("@Str", creature.Str);
            cmd.Parameters.AddWithValue("@Dex", creature.Dex);
            cmd.Parameters.AddWithValue("@Con", creature.Con);
            cmd.Parameters.AddWithValue("@Int", creature.Int);
            cmd.Parameters.AddWithValue("@Wis", creature.Wis);
            cmd.Parameters.AddWithValue("@Cha", creature.Cha);
            cmd.Parameters.AddWithValue("@Skills", creature.Skills != null ? string.Join(", ", creature.Skills) : "");
            cmd.Parameters.AddWithValue("@Senses", creature.Senses ?? "");
            cmd.Parameters.AddWithValue("@Languages", creature.Languages ?? "");
            cmd.Parameters.AddWithValue("@Immunities", creature.Immunities ?? "");
            cmd.Parameters.AddWithValue("@Resistances", creature.Resistances ?? "");
            cmd.Parameters.AddWithValue("@Vulnerabilities", creature.Vulnerabilities ?? "");
            cmd.Parameters.AddWithValue("@Traits", creature.Traits ?? "");
            cmd.Parameters.AddWithValue("@Notes", creature.Notes ?? "");
            cmd.Parameters.AddWithValue("@IconPath", creature.IconPath ?? "");
            cmd.Parameters.AddWithValue("@SizeInSquares", creature.SizeInSquares);

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task DeleteCreatureAsync(string creatureId)
        {
            await EnsureConnectionAsync();

            // Remove from categories first
            var removeCatCmd = _conn.CreateCommand();
            removeCatCmd.CommandText = "DELETE FROM CreatureCategories WHERE CreatureId = @CreatureId";
            removeCatCmd.Parameters.AddWithValue("@CreatureId", creatureId);
            await removeCatCmd.ExecuteNonQueryAsync();

            // Remove from favorites
            var removeFavCmd = _conn.CreateCommand();
            removeFavCmd.CommandText = "DELETE FROM Favorites WHERE CreatureId = @CreatureId";
            removeFavCmd.Parameters.AddWithValue("@CreatureId", creatureId);
            await removeFavCmd.ExecuteNonQueryAsync();

            // Delete creature
            var deleteCmd = _conn.CreateCommand();
            deleteCmd.CommandText = "DELETE FROM Creatures WHERE Id = @Id";
            deleteCmd.Parameters.AddWithValue("@Id", creatureId);
            await deleteCmd.ExecuteNonQueryAsync();
        }

        #endregion

        #region Tag Operations

        public async Task<int> GetOrCreateTagIdAsync(string tagName)
        {
            await EnsureConnectionAsync();

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
            await EnsureConnectionAsync();

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
            await EnsureConnectionAsync();

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
            await EnsureConnectionAsync();

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

            await EnsureConnectionAsync();

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

        public async Task<List<string>> GetAllTypesAsync()
        {
            await EnsureConnectionAsync();

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
            await EnsureInitializedAsync();

            var cmd = _conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM Creatures";
            return Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }

        /// <summary>
        /// Gets all creatures from the database
        /// </summary>
        public async Task<List<Token>> GetAllCreaturesAsync(int limit = 1000)
        {
            await EnsureConnectionAsync();

            var creatures = new List<Token>();

            try
            {
                var cmd = _conn.CreateCommand();
                cmd.CommandText = "SELECT * FROM Creatures ORDER BY Name LIMIT @Limit";
                cmd.Parameters.AddWithValue("@Limit", limit);

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    creatures.Add(ReadCreatureFromReader(reader));
                }

                System.Diagnostics.Debug.WriteLine($"GetAllCreaturesAsync: Found {creatures.Count} creatures");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetAllCreaturesAsync: {ex.Message}");
            }

            return creatures;
        }

        #endregion

        #region JSON Import

        public async Task<int> ImportFromJsonFileAsync(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"JSON file not found: {filePath}");

            var category = "dnd5e-srd"; // Default category for imported creatures
            int importedCount = 0;
            int skippedCount = 0;
            int errorCount = 0;

            try
            {
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

                using (JsonDocument document = JsonDocument.Parse(json))
                {
                    JsonElement root = document.RootElement;

                    if (root.ValueKind == JsonValueKind.Array)
                    {
                        int totalInFile = 0;
                        foreach (JsonElement creatureElement in root.EnumerateArray())
                        {
                            totalInFile++;
                            try
                            {
                                var token = ParseCreatureFromJsonElement(creatureElement);

                                if (token == null)
                                {
                                    System.Diagnostics.Debug.WriteLine($"  Creature #{totalInFile}: Parse returned null");
                                    skippedCount++;
                                    continue;
                                }

                                if (string.IsNullOrWhiteSpace(token.Name))
                                {
                                    System.Diagnostics.Debug.WriteLine($"  Creature #{totalInFile}: No name found");
                                    skippedCount++;
                                    continue;
                                }

                                // Log what we're about to import
                                System.Diagnostics.Debug.WriteLine($"  Importing: {token.Name} | Type: '{token.Type}' | CR: {token.ChallengeRating}");

                                await AddCreatureAsync(token, category, filePath);
                                importedCount++;
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"  Error on creature #{totalInFile}: {ex.Message}");
                                errorCount++;
                            }
                        }

                        System.Diagnostics.Debug.WriteLine($"File {Path.GetFileName(filePath)}: Total={totalInFile}, Imported={importedCount}, Skipped={skippedCount}, Errors={errorCount}");
                    }
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

        /// <summary>
        /// Adds a creature from a JSON element. 
        /// Only skips if the exact same JSON ID already exists in the database.
        /// </summary>
        public async Task<AddCreatureResult> AddCreatureFromJsonElementAsync(
            System.Text.Json.JsonElement element,
            string category,
            string sourceFile)
        {
            try
            {
                var token = ParseCreatureFromJsonElement(element);

                if (token == null || string.IsNullOrWhiteSpace(token.Name))
                    return AddCreatureResult.Skipped;

                // Check if this exact ID already exists
                var existingCmd = _conn.CreateCommand();
                existingCmd.CommandText = "SELECT COUNT(*) FROM Creatures WHERE Id = @Id";
                existingCmd.Parameters.AddWithValue("@Id", token.Id.ToString());
                var existingCount = Convert.ToInt32(await existingCmd.ExecuteScalarAsync());

                if (existingCount > 0)
                {
                    // This exact ID already exists - it's a duplicate
                    return AddCreatureResult.Skipped;
                }

                // Add the creature with its original ID
                await AddCreatureAsync(token, category, sourceFile);
                return AddCreatureResult.Added;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error adding creature: {ex.Message}");
                return AddCreatureResult.Error;
            }
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

            try
            {
                // Parse each property safely
                token.Id = GetGuidProperty(element, "Id") ?? Guid.NewGuid();

                token.Name = GetStringProperty(element, "Name")
                          ?? GetStringProperty(element, "name")
                          ?? "Unknown";

                // Handle Type - might be a string or an object with nested properties
                token.Type = ParseTypeProperty(element);

                token.Size = GetStringProperty(element, "Size")
                          ?? GetStringProperty(element, "size")
                          ?? "";

                token.Alignment = GetStringProperty(element, "Alignment")
                               ?? GetStringProperty(element, "alignment")
                               ?? "";

                // Handle ChallengeRating - might be "CR" or "ChallengeRating", string or number
                token.ChallengeRating = ParseChallengeRating(element);

                // For int properties, try each variant separately (can't chain ?? with int)
                token.ArmorClass = GetIntProperty(element, "ArmorClass", 0);
                if (token.ArmorClass == 0) token.ArmorClass = GetIntProperty(element, "AC", 0);
                if (token.ArmorClass == 0) token.ArmorClass = GetIntProperty(element, "ac", 10);

                token.MaxHP = GetIntProperty(element, "MaxHP", 0);
                if (token.MaxHP == 0) token.MaxHP = GetIntProperty(element, "HP", 0);
                if (token.MaxHP == 0) token.MaxHP = GetIntProperty(element, "hp", 0);
                if (token.MaxHP == 0) token.MaxHP = GetIntProperty(element, "HitPoints", 1);
                token.HP = token.MaxHP;

                token.HitDice = GetStringProperty(element, "HitDice")
                             ?? GetStringProperty(element, "hitDice")
                             ?? "";

                token.InitiativeModifier = GetIntProperty(element, "InitiativeMod", 0);
                if (token.InitiativeModifier == 0)
                    token.InitiativeModifier = GetIntProperty(element, "InitiativeModifier", 0);

                token.Speed = GetStringProperty(element, "Speed")
                           ?? GetStringProperty(element, "speed")
                           ?? "30 ft.";

                // Ability scores - try uppercase first, then lowercase
                token.Str = GetIntProperty(element, "Str", 0);
                if (token.Str == 0) token.Str = GetIntProperty(element, "str", 10);

                token.Dex = GetIntProperty(element, "Dex", 0);
                if (token.Dex == 0) token.Dex = GetIntProperty(element, "dex", 10);

                token.Con = GetIntProperty(element, "Con", 0);
                if (token.Con == 0) token.Con = GetIntProperty(element, "con", 10);

                token.Int = GetIntProperty(element, "Int", 0);
                if (token.Int == 0) token.Int = GetIntProperty(element, "int", 10);

                token.Wis = GetIntProperty(element, "Wis", 0);
                if (token.Wis == 0) token.Wis = GetIntProperty(element, "wis", 10);

                token.Cha = GetIntProperty(element, "Cha", 0);
                if (token.Cha == 0) token.Cha = GetIntProperty(element, "cha", 10);

                token.Senses = GetStringProperty(element, "Senses") ?? "";
                token.Languages = GetStringProperty(element, "Languages") ?? "";
                token.Immunities = GetStringProperty(element, "Immunities") ?? "";
                token.Resistances = GetStringProperty(element, "Resistances") ?? "";
                token.Vulnerabilities = GetStringProperty(element, "Vulnerabilities") ?? "";
                token.Traits = GetStringProperty(element, "Traits") ?? "";
                token.Notes = GetStringProperty(element, "Notes") ?? "";
                token.IconPath = GetStringProperty(element, "IconPath") ?? "";
                token.SizeInSquares = GetIntProperty(element, "SizeInSquares", 1);

                // Parse Skills array or string
                token.Skills = ParseSkillsProperty(element);

                // Parse Actions
                token.Actions = ParseActionsArray(element, "Actions") ?? new List<Models.Combat.Action>();
                token.BonusActions = ParseActionsArray(element, "BonusActions") ?? new List<Models.Combat.Action>();
                token.Reactions = ParseActionsArray(element, "Reactions") ?? new List<Models.Combat.Action>();
                token.LegendaryActions = ParseActionsArray(element, "LegendaryActions") ?? new List<Models.Combat.Action>();

                token.Tags = new List<string>();

                System.Diagnostics.Debug.WriteLine($"Parsed creature: {token.Name}, Type: {token.Type}, CR: {token.ChallengeRating}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error parsing creature: {ex.Message}");
            }

            return token;
        }

        /// <summary>
        /// Parses the Type property which might be a simple string or contain parentheses like "Humanoid (Goblinoid)"
        /// </summary>
        private string ParseTypeProperty(JsonElement element)
        {
            // Try direct "Type" property first
            var typeValue = GetStringProperty(element, "Type") ?? GetStringProperty(element, "type");

            if (!string.IsNullOrEmpty(typeValue))
            {
                // The type is already a string - return it as-is (preserving parentheses)
                return typeValue.Trim();
            }

            // Some JSON formats have separate "type" and "subtype" fields
            var mainType = GetStringProperty(element, "type") ?? GetStringProperty(element, "creature_type");
            var subType = GetStringProperty(element, "subtype") ?? GetStringProperty(element, "creature_subtype");

            if (!string.IsNullOrEmpty(mainType))
            {
                if (!string.IsNullOrEmpty(subType))
                {
                    return $"{mainType} ({subType})";
                }
                return mainType;
            }

            // Try parsing a nested "meta" or "type" object
            if (element.TryGetProperty("meta", out JsonElement metaElement))
            {
                var metaType = GetStringProperty(metaElement, "type");
                if (!string.IsNullOrEmpty(metaType))
                    return metaType;
            }

            return "";
        }

        /// <summary>
        /// Parses Challenge Rating which might be in various formats
        /// </summary>
        private string ParseChallengeRating(JsonElement element)
        {
            // Try string property first
            var crString = GetStringProperty(element, "ChallengeRating")
                        ?? GetStringProperty(element, "CR")
                        ?? GetStringProperty(element, "cr")
                        ?? GetStringProperty(element, "challenge_rating");

            if (!string.IsNullOrEmpty(crString))
                return crString.Trim();

            // Try numeric property
            if (element.TryGetProperty("ChallengeRating", out JsonElement crElement) ||
                element.TryGetProperty("CR", out crElement) ||
                element.TryGetProperty("cr", out crElement))
            {
                if (crElement.ValueKind == JsonValueKind.Number)
                {
                    if (crElement.TryGetDouble(out double crValue))
                    {
                        // Handle fractions
                        if (crValue == 0.125) return "1/8";
                        if (crValue == 0.25) return "1/4";
                        if (crValue == 0.5) return "1/2";
                        return crValue.ToString();
                    }
                }
            }

            // Try nested "challenge" object (some formats use this)
            if (element.TryGetProperty("challenge", out JsonElement challengeElement))
            {
                var nestedCr = GetStringProperty(challengeElement, "rating");
                if (!string.IsNullOrEmpty(nestedCr))
                    return nestedCr;
            }

            return "";
        }

        /// <summary>
        /// Parses Skills which might be an array or a string
        /// </summary>
        private List<string> ParseSkillsProperty(JsonElement element)
        {
            var skills = new List<string>();

            // Try as array first
            if (element.TryGetProperty("Skills", out JsonElement skillsElement))
            {
                if (skillsElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var skill in skillsElement.EnumerateArray())
                    {
                        if (skill.ValueKind == JsonValueKind.String)
                        {
                            var skillStr = skill.GetString();
                            if (!string.IsNullOrWhiteSpace(skillStr))
                                skills.Add(skillStr);
                        }
                    }
                }
                else if (skillsElement.ValueKind == JsonValueKind.String)
                {
                    // It's a comma-separated string
                    var skillsStr = skillsElement.GetString();
                    if (!string.IsNullOrWhiteSpace(skillsStr))
                    {
                        skills.AddRange(skillsStr.Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(s => s.Trim()));
                    }
                }
            }

            return skills;
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

        private List<Models.Combat.Action> ParseActionsArray(JsonElement element, string propertyName)
        {
            var actions = new List<Models.Combat.Action>();

            if (element.TryGetProperty(propertyName, out JsonElement actionsElement) &&
                actionsElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var actionElement in actionsElement.EnumerateArray())
                {
                    var action = new Models.Combat.Action
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

        private Token ReadCreatureFromReader(Microsoft.Data.Sqlite.SqliteDataReader reader)
        {
            return new Token
            {
                Id = Guid.TryParse(GetStringOrDefault(reader, "Id"), out var id) ? id : Guid.NewGuid(),
                Name = GetStringOrDefault(reader, "Name"),
                Size = GetStringOrDefault(reader, "Size"),
                Type = GetStringOrDefault(reader, "Type"),
                Alignment = GetStringOrDefault(reader, "Alignment"),
                ChallengeRating = GetStringOrDefault(reader, "ChallengeRating"),
                ArmorClass = GetIntOrDefault(reader, "ArmorClass"),
                MaxHP = GetIntOrDefault(reader, "MaxHP"),
                HP = GetIntOrDefault(reader, "MaxHP"),
                HitDice = GetStringOrDefault(reader, "HitDice"),
                InitiativeModifier = GetIntOrDefault(reader, "InitiativeModifier"),
                Speed = GetStringOrDefault(reader, "Speed"),
                Str = GetIntOrDefault(reader, "Str", 10),
                Dex = GetIntOrDefault(reader, "Dex", 10),
                Con = GetIntOrDefault(reader, "Con", 10),
                Int = GetIntOrDefault(reader, "Int", 10),
                Wis = GetIntOrDefault(reader, "Wis", 10),
                Cha = GetIntOrDefault(reader, "Cha", 10),
                Traits = GetStringOrDefault(reader, "Traits"),
                SizeInSquares = GetIntOrDefault(reader, "SizeInSquares", 1)
            };
        }

        private string GetStringOrDefault(Microsoft.Data.Sqlite.SqliteDataReader reader, string column, string defaultValue = "")
        {
            try
            {
                var ordinal = reader.GetOrdinal(column);
                return reader.IsDBNull(ordinal) ? defaultValue : reader.GetString(ordinal);
            }
            catch { return defaultValue; }
        }

        private int GetIntOrDefault(Microsoft.Data.Sqlite.SqliteDataReader reader, string column, int defaultValue = 0)
        {
            try
            {
                var ordinal = reader.GetOrdinal(column);
                return reader.IsDBNull(ordinal) ? defaultValue : reader.GetInt32(ordinal);
            }
            catch { return defaultValue; }
        }

        public void Dispose()
        {
            _conn?.Close();
            _conn?.Dispose();
        }

        #endregion
    }

    public class CreatureSummary
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Size { get; set; }
        public string Type { get; set; }
        public string ChallengeRating { get; set; }
        public int ArmorClass { get; set; }
        public int MaxHP { get; set; }
        public string IconPath { get;set; }
    }

    public enum AddCreatureResult
    {
        Added,
        Skipped,
        Error
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

Imports System.IO
Imports Microsoft.Data.Sqlite
Imports Dapper

Namespace Services
    Public Class DatabaseService
        Private ReadOnly _dbPath As String
        Private ReadOnly _connectionString As String

        Public Sub New()
            ' Set the database path to the application data folder
            Dim appData = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data")
            If Not Directory.Exists(appData) Then
                Directory.CreateDirectory(appData)
            End If

            _dbPath = Path.Combine(appData, "SmartTillV2.db")
            _connectionString = $"Data Source={_dbPath}"
            
            ' Initialize the database
            InitializeDatabase()
        End Sub

        Public Function GetConnection() As SqliteConnection
            Return New SqliteConnection(_connectionString)
        End Function

        Private Sub InitializeDatabase()
            Using db = GetConnection()
                db.Open()

                ' Create Users Table
                db.Execute("CREATE TABLE IF NOT EXISTS Users (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Username TEXT NOT NULL UNIQUE,
                    PasswordHash TEXT NOT NULL,
                    Role TEXT NOT NULL,
                    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
                );")

                ' Migration: Add Permissions column if it doesn't exist
                Dim tableInfo = db.Query("PRAGMA table_info(Users)")
                Dim hasPermissionsColumn = tableInfo.Any(Function(row) DirectCast(row.name, String).Equals("Permissions", StringComparison.OrdinalIgnoreCase))

                If Not hasPermissionsColumn Then
                    db.Execute("ALTER TABLE Users ADD COLUMN Permissions TEXT;")
                End If

                ' Seed Admin User if not exists
                Dim adminExists = db.ExecuteScalar(Of Integer)("SELECT COUNT(*) FROM Users WHERE Role = 'Admin'")
                If adminExists = 0 Then
                    Dim hashedPassword = BCrypt.Net.BCrypt.HashPassword("admin123")
                    db.Execute("INSERT INTO Users (Username, PasswordHash, Role, Permissions) VALUES (@Username, @PasswordHash, @Role, @Permissions)",
                                New With {.Username = "admin", .PasswordHash = hashedPassword, .Role = "Admin", .Permissions = "{""All"": true}"})
                End If

                ' Create Products Table
                db.Execute("CREATE TABLE IF NOT EXISTS Products (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL,
                    Barcode TEXT UNIQUE,
                    Price DECIMAL(10,2) NOT NULL,
                    StockQuantity INTEGER NOT NULL DEFAULT 0,
                    Category TEXT
                );")

                ' Create Sales Table
                db.Execute("CREATE TABLE IF NOT EXISTS Sales (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    TotalAmount DECIMAL(10,2) NOT NULL,
                    CashierId INTEGER,
                    SaleDate DATETIME DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (CashierId) REFERENCES Users(Id)
                );")

                ' Create SaleDetails Table
                db.Execute("CREATE TABLE IF NOT EXISTS SaleDetails (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    SaleId INTEGER NOT NULL,
                    ProductId INTEGER NOT NULL,
                    Quantity INTEGER NOT NULL,
                    UnitPrice DECIMAL(10,2) NOT NULL,
                    FOREIGN KEY (SaleId) REFERENCES Sales(Id),
                    FOREIGN KEY (ProductId) REFERENCES Products(Id)
                );")
            End Using
        End Sub

        Public Function AuthenticateUser(username As String, password As String) As Models.User
            Using db = GetConnection()
                db.Open()
                Dim user = db.QueryFirstOrDefault(Of Models.User)("SELECT * FROM Users WHERE Username = @Username", New With {.Username = username})
                
                If user IsNot Nothing AndAlso BCrypt.Net.BCrypt.Verify(password, user.PasswordHash) Then
                    Return user
                End If
            End Using
            Return Nothing
        End Function

        Public Function CreateUser(username As String, password As String, role As String, permissions As String) As Boolean
            Try
                Dim hashedPassword = BCrypt.Net.BCrypt.HashPassword(password)
                Using db = GetConnection()
                    db.Open()
                    db.Execute("INSERT INTO Users (Username, PasswordHash, Role, Permissions) VALUES (@Username, @PasswordHash, @Role, @Permissions)",
                                New With {.Username = username, .PasswordHash = hashedPassword, .Role = role, .Permissions = permissions})
                    Return True
                End Using
            Catch ex As Exception
                Return False
            End Try
        End Function

        Public Function VerifyDatabase() As String
            Try
                If Not File.Exists(_dbPath) Then
                    Return "Database file not found."
                End If

                Using db = GetConnection()
                    db.Open()
                    Dim count = db.ExecuteScalar(Of Integer)("SELECT COUNT(*) FROM Users")
                    Return $"Database verified successfully. Tables exist and are accessible. Location: {_dbPath}"
                End Using
            Catch ex As Exception
                Return $"Database verification failed: {ex.Message}"
            End Try
        End Function
    End Class
End Namespace

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

        Public Function VerifyDatabase() As String
            Try
                If Not File.Exists(_dbPath) Then
                    Return "Database file not found."
                End If

                Using db = GetConnection()
                    db.Open()
                    ' Try a simple query
                    Dim count = db.ExecuteScalar(Of Integer)("SELECT COUNT(*) FROM Users")
                    Return $"Database verified successfully. Tables exist and are accessible. Location: {_dbPath}"
                End Using
            Catch ex As Exception
                Return $"Database verification failed: {ex.Message}"
            End Try
        End Function
    End Class
End Namespace

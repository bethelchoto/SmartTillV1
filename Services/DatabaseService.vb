Imports System.IO
Imports Microsoft.Data.Sqlite
Imports Dapper
Imports System.Linq
Imports System.Collections.Generic

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

                ' Create Categories Table
                db.Execute("CREATE TABLE IF NOT EXISTS Categories (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL UNIQUE
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

        Public Function GetAllUsers() As List(Of Models.User)
            Using db = GetConnection()
                db.Open()
                Return db.Query(Of Models.User)("SELECT * FROM Users").ToList()
            End Using
        End Function

        Public Function DeleteUser(id As Integer) As Boolean
            Try
                Using db = GetConnection()
                    db.Open()
                    db.Execute("DELETE FROM Users WHERE Id = @Id", New With {.Id = id})
                    Return True
                End Using
            Catch ex As Exception
                Return False
            End Try
        End Function

        Public Function UpdateUser(user As Models.User) As Boolean
            Try
                Using db = GetConnection()
                    db.Open()
                    db.Execute("UPDATE Users SET Role = @Role, Permissions = @Permissions WHERE Id = @Id",
                                New With {.Role = user.Role, .Permissions = user.Permissions, .Id = user.Id})
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
        Public Function GetAllProducts() As List(Of Models.Product)
            Using db = GetConnection()
                db.Open()
                Return db.Query(Of Models.Product)("SELECT * FROM Products").ToList()
            End Using
        End Function

        Public Function CreateProduct(name As String, barcode As String, price As Decimal, stock As Integer, category As String) As Boolean
            Try
                Using db = GetConnection()
                    db.Open()
                    db.Execute("INSERT INTO Products (Name, Barcode, Price, StockQuantity, Category) VALUES (@Name, @Barcode, @Price, @Stock, @Category)",
                                New With {.Name = name, .Barcode = barcode, .Price = price, .Stock = stock, .Category = category})
                    Return True
                End Using
            Catch ex As Exception
                Return False
            End Try
        End Function

        Public Function DeleteProduct(id As Integer) As Boolean
            Try
                Using db = GetConnection()
                    db.Open()
                    db.Execute("DELETE FROM Products WHERE Id = @Id", New With {.Id = id})
                    Return True
                End Using
            Catch ex As Exception
                Return False
            End Try
        End Function

        Public Function UpdateProduct(product As Models.Product) As Boolean
            Try
                Using db = GetConnection()
                    db.Open()
                    db.Execute("UPDATE Products SET Name = @Name, Barcode = @Barcode, Price = @Price, StockQuantity = @StockQuantity, Category = @Category WHERE Id = @Id",
                                New With {.Name = product.Name, .Barcode = product.Barcode, .Price = product.Price, .StockQuantity = product.StockQuantity, .Category = product.Category, .Id = product.Id})
                    Return True
                End Using
            Catch ex As Exception
                Return False
            End Try
        End Function

        ' Category CRUD
        Public Function GetAllCategories() As List(Of Models.Category)
            Using db = GetConnection()
                db.Open()
                Return db.Query(Of Models.Category)("SELECT * FROM Categories").ToList()
            End Using
        End Function

        Public Function CreateCategory(name As String) As Boolean
            Try
                Using db = GetConnection()
                    db.Open()
                    db.Execute("INSERT INTO Categories (Name) VALUES (@Name)", New With {.Name = name})
                    Return True
                End Using
            Catch ex As Exception
                Return False
            End Try
        End Function

        Public Function DeleteCategory(id As Integer) As Boolean
            Try
                Using db = GetConnection()
                    db.Open()
                    db.Execute("DELETE FROM Categories WHERE Id = @Id", New With {.Id = id})
                    Return True
                End Using
            Catch ex As Exception
                Return False
            End Try
        End Function
        ' Sale Processing
        Public Function ProcessSale(sale As Models.Sale, details As List(Of Models.SaleDetail)) As Boolean
            Using db = GetConnection()
                db.Open()
                Using transaction = db.BeginTransaction()
                    Try
                        ' 1. Insert Sale record and get its ID
                        Dim saleId = db.QuerySingle(Of Integer)("INSERT INTO Sales (TotalAmount, CashierId, SaleDate) VALUES (@TotalAmount, @CashierId, @SaleDate); SELECT last_insert_rowid();",
                            New With {.TotalAmount = sale.TotalAmount, .CashierId = sale.CashierId, .SaleDate = sale.SaleDate}, transaction)

                        ' 2. Insert Sale Details and update stock for each item
                        For Each detail In details
                            detail.SaleId = saleId
                            db.Execute("INSERT INTO SaleDetails (SaleId, ProductId, Quantity, UnitPrice) VALUES (@SaleId, @ProductId, @Quantity, @UnitPrice)",
                                detail, transaction)

                            ' Reduce stock amount
                            db.Execute("UPDATE Products SET StockQuantity = StockQuantity - @Qty WHERE Id = @Id",
                                New With {.Qty = detail.Quantity, .Id = detail.ProductId}, transaction)
                        Next

                        transaction.Commit()
                        Return True
                    Catch ex As Exception
                        transaction.Rollback()
                        Return False
                    End Try
                End Using
            End Using
        End Function

        Public Function GetProductByBarcode(barcode As String) As Models.Product
            Using db = GetConnection()
                db.Open()
                Return db.QueryFirstOrDefault(Of Models.Product)("SELECT * FROM Products WHERE Barcode = @Barcode", New With {.Barcode = barcode})
            End Using
        End Function
    End Class
End Namespace
Namespace Models
    Public Class User
        Public Property Id As Integer
        Public Property Username As String
        Public Property PasswordHash As String
        Public Property Role As String ' Admin, Tailor, Manager
        Public Property Permissions As String ' JSON string of permissions
        Public Property CreatedAt As DateTime = DateTime.Now

        ' Helper to check if user is admin
        Public ReadOnly Property IsAdmin As Boolean
            Get
                Return Role = "Admin"
            End Get
        End Property
    End Class
End Namespace

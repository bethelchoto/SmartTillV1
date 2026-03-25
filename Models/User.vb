Namespace Models
    Public Class User
        Public Property Id As Integer
        Public Property Username As String
        Public Property PasswordHash As String
        Public Property Role As String ' Admin, TillOperator
        Public Property CreatedAt As DateTime = DateTime.Now
    End Class
End Namespace

Public Class LoginWindow
    Private ReadOnly _viewModel As ViewModels.LoginViewModel

    Public Sub New()
        InitializeComponent()
        _viewModel = New ViewModels.LoginViewModel()
        Me.DataContext = _viewModel
        
        AddHandler _viewModel.LoginSuccessful, AddressOf OnLoginSuccessful
    End Sub

    Private Sub Login_Click(sender As Object, e As RoutedEventArgs)
        _viewModel.Login(txtPassword.Password)
    End Sub

    Private Sub OnLoginSuccessful(user As Models.User)
        ' Open MainWindow and close this one
        Dim main = New MainWindow()
        main.Show()
        Me.Close()
    End Sub
End Class

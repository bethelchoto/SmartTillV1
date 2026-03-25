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

    Private Sub Close_Click(sender As Object, e As RoutedEventArgs)
        Application.Current.Shutdown()
    End Sub

    Private Sub Minimize_Click(sender As Object, e As RoutedEventArgs)
        Me.WindowState = WindowState.Minimized
    End Sub

    Private Sub Window_MouseDown(sender As Object, e As MouseButtonEventArgs)
        If e.ChangedButton = MouseButton.Left Then
            Me.DragMove()
        End If
    End Sub

    Private Sub txtPassword_PasswordChanged(sender As Object, e As RoutedEventArgs)
        If txtPasswordWatermark IsNot Nothing Then
            If String.IsNullOrEmpty(txtPassword.Password) Then
                txtPasswordWatermark.Visibility = Visibility.Visible
            Else
                txtPasswordWatermark.Visibility = Visibility.Collapsed
            End If
        End If
    End Sub
End Class
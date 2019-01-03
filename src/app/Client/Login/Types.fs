module Client.Login.Types

open TimeOff.AuthTypes

type LoginState =
    | LoggedOut
    | LoggedIn of UserData

type Model = { 
    State : LoginState
    Login : Login
    ErrorMsg : string }

type Msg =
  | SetUserName of string
  | SetPassword of string
  | AuthSuccess of UserData
  | AuthError of exn
  | ClickLogIn
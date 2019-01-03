module Client.Login.View

open System
open Fable.Core.JsInterop
open Fable.Helpers.React
open Fable.Helpers.React.Props
open Fulma
open Fable.FontAwesome

open Client
open Types

let root model (dispatch: Msg -> unit) = 
  let buttonActive = if String.IsNullOrEmpty model.Login.UserName || String.IsNullOrEmpty model.Login.Password then Button.Disabled true else Button.Color IsPrimary

  let notification =
    if not (String.IsNullOrEmpty model.ErrorMsg) then
      Notification.notification [ Notification.Color IsDanger ] [
        div [] [ str model.ErrorMsg ]
        div [] [ str "Log in with either 'employee1', 'employee2', 'manager' or using your login as your password." ]
      ]
    else
      Notification.notification [ Notification.Color IsInfo ] [
        str "Log in with either 'employee1', 'employee2', 'manager', using your login as your password."]

  match model.State with
  | LoggedIn _ ->
      div [] [
        h3 [] [ str (sprintf "You're logged in as %s." model.Login.UserName) ]
      ]

  | LoggedOut ->
    form [ ] [
      notification

      Field.div [ ]
        [ Label.label [ ]
            [ str "Username" ]
          Control.div [ Control.HasIconLeft ]
            [ Input.input [ Input.Type Input.Text
                            Input.Id "username"
                            Input.Placeholder "Username"
                            Input.DefaultValue model.Login.UserName
                            Input.Props [
                              OnChange (fun ev -> dispatch (SetUserName !!ev.target?value))
                              AutoFocus true ] ]
              Icon.icon [ Icon.Size IsSmall; Icon.IsLeft ] [ Fa.i [ Fa.Regular.Angry ] [ ] ] 
            ] 
        ]

      Field.div [ ]
        [ Label.label [ ]
            [ str "Password" ]
          Control.div [ Control.HasIconLeft ]
            [ Input.input [ Input.Type Input.Password
                            Input.Placeholder "Password"
                            Input.Id "password"
                            Input.DefaultValue model.Login.Password
                            Input.Props [
                              Key ("password_" + model.Login.PasswordId.ToString())
                              OnChange (fun ev -> dispatch (SetPassword !!ev.target?value))
                              OnEnter ClickLogIn dispatch ] ]
              Icon.icon [ Icon.Size IsSmall; Icon.IsLeft ] [ Fa.i [ Fa.Regular.Keyboard ] [ ] ] 
            ] 
        ]
       
      div [ ClassName "text-center" ] [
          Button.a [buttonActive; Button.OnClick  (fun _ -> dispatch ClickLogIn)] [ str "Log In" ]
      ]
    ]    

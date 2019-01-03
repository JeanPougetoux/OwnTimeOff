module Client.View

open Fable.Helpers.React
open Fable.Helpers.React.Props
open Fulma

open Client.Types

/// Constructs the view for a page given the model and dispatcher.
let root model dispatch =
  let pageHtml =
    function
    | Page.Home -> [ Home.View.root model.Home (HomeMsg >> dispatch) ]
    | Page.Login ->
      match model.TransientPageModel with
      | LoginModel loginModel -> [ Login.View.root loginModel (LoginMsg >> dispatch) ]
      | _ -> []
    | Page.Employees -> [ Employees.View.root dispatch ]
    | Page.Balance _ ->
      match model.TransientPageModel with
      | BalanceModel balanceModel -> [ Balance.View.root balanceModel (BalanceMsg >> dispatch) ]
      | _ -> []
    | Page.About -> [ About.View.root ]

  div
    []
    [ Navbar.View.view model.Navigation (GlobalMsg >> dispatch)
      div
        [ ClassName "section" ]
        [ div
            [ ClassName "container" ]
            [ div
                [ ClassName "columns" ]
                [ div
                    [ ClassName "column is-3" ]
                    [ Menu.View.view model.Navigation ]
                  div
                    [ ClassName "column" ]
                    (pageHtml model.Navigation.CurrentPage) ] ] ] ]



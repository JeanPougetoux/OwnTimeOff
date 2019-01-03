module Client.Counter.View

open Fable.Helpers.React
open Fable.Helpers.React.Props

open Types
open Fulma

let simpleButton txt action dispatch =
  Column.column [ Column.Width (Screen.All, Column.IsNarrow) ]
    [ Button.a [ Button.OnClick (fun _ -> action |> dispatch) ]
        [ str txt ] ]

let root model dispatch =
  Box.box' [ CustomClass "counter" ]
    [
      Columns.columns [ Columns.IsVCentered ]
        [ Column.column [ ]
            [ str (sprintf "Counter value: %i" model.Counter) ]
          simpleButton "+1" Increment dispatch
          simpleButton "-1" Decrement dispatch ]
    ]

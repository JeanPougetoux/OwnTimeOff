module Client.Counter.State

open Elmish
open Types
let init () : Model * Cmd<Msg> =
  { Counter  = 0 }, []

let update msg model =
  match msg with
  | Increment ->
      { model with Counter = model.Counter  + 1 }, []
  | Decrement ->
      { model with Counter = model.Counter  - 1 }, []

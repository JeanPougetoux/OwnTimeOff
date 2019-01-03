module Client.About.View

open Fable.Helpers.React
open Fulma

let root =
  Container.container [ ]
    [ Content.content [ ]
        [ Heading.h3 [ ]
            [ str "About page" ]
          p [ ]
            [ str "This is a starter project to use in an F# coding assignment working with Fable + Elmish + React on the front-end, Giraffe on the back-end." ] ] ]

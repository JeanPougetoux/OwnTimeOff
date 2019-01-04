/// Login web part and functions for API web part request authorisation with JWT.
module ServerCode.Auth

open TimeOff

open Giraffe
open RequestErrors
open Microsoft.AspNetCore.Http
open FSharp.Control.Tasks.V2

let private createUserData (identity: ServerTypes.Identity) =
    {
        UserName = identity.UserName
        Token    = ServerCode.JsonWebToken.encode identity
    } : AuthTypes.UserData

let private tryGetValidIdentityFromLogin (login: AuthTypes.Login) : ServerTypes.Identity option =
    if login.Password = login.UserName then
        match login.UserName with
        | "manager" -> Some { UserName = login.UserName; UserId = 0; Roles = ["manager"] }
        | "employee1" -> Some { UserName = login.UserName; UserId = 1; Roles = List.empty }
        | "employee2" -> Some { UserName = login.UserName; UserId = 2; Roles = List.empty }
        | "employee3" -> Some { UserName = login.UserName; UserId = 3; Roles = List.empty }
        | _ -> None
    else
        None

/// Authenticates a user and returns a token in the HTTP body.
let login : HttpHandler =
    fun (next : HttpFunc) (ctx : HttpContext) ->
        task {
            let! login = ctx.BindJsonAsync<AuthTypes.Login>()
            return!
                match tryGetValidIdentityFromLogin login with
                | Some identity ->
                    let data = createUserData identity
                    ctx.WriteJsonAsync data
                | None -> UNAUTHORIZED "Bearer" "" (sprintf "User '%s' can't be logged in." login.UserName) next ctx
        }

let private missingToken = RequestErrors.BAD_REQUEST "Request doesn't contain a JSON Web Token"
let private invalidToken = RequestErrors.FORBIDDEN "Accessing this API is not allowed"

/// Checks if the HTTP request has a valid JWT token for API.
/// On success it will invoke the given `f` function by passing in the valid token.
let requiresJwtTokenForAPI f : HttpHandler =
    fun (next : HttpFunc) (ctx : HttpContext) ->
        (match ctx.TryGetRequestHeader "Authorization" with
        | Some authHeader ->
            let jwt = authHeader.Replace("Bearer ", "")
            match JsonWebToken.isValid jwt with
            | Some token -> f token
            | None -> invalidToken
        | None -> missingToken) next ctx

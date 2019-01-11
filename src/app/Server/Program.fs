module ServerCode.App

open TimeOff
open EventStorage

open System
open System.IO
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Cors.Infrastructure
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Logging
open Microsoft.Extensions.DependencyInjection
open Giraffe
open Giraffe.HttpStatusCodeHandlers.RequestErrors
open FSharp.Control.Tasks

// ---------------------------------
// Handlers
// ---------------------------------

module HttpHandlers =

    open Microsoft.AspNetCore.Http

    [<CLIMutable>]
    type UserAndRequestId = {
        UserId: UserId
        RequestId: Guid
    }

    let getUserFromIdentity (identity: ServerTypes.Identity) : User =
        if identity.Roles |> Seq.contains "manager" then
            Manager
        else
            Employee identity.UserId

    let requestTimeOff (handleCommand: User -> Command -> Result<RequestEvent list, string>) (identity: ServerTypes.Identity) =
        fun (next: HttpFunc) (ctx: HttpContext) ->
            task {
                let! timeOffRequest = ctx.BindJsonAsync<TimeOffRequest>()
                let user = getUserFromIdentity identity
                let command = RequestTimeOff timeOffRequest
                let result = handleCommand user command
                match result with
                | Ok _ -> return! json timeOffRequest next ctx
                | Error message ->
                    return! (BAD_REQUEST message) next ctx
            }

    let validateRequest (handleCommand: User -> Command -> Result<RequestEvent list, string>) (identity: ServerTypes.Identity) =
        fun (next: HttpFunc) (ctx: HttpContext) ->
            task {
                let userAndRequestId = ctx.BindQueryString<UserAndRequestId>()
                let user = getUserFromIdentity identity
                let command = ValidateRequest (userAndRequestId.UserId, userAndRequestId.RequestId)
                let result = handleCommand user command
                match result with
                | Ok [RequestValidated timeOffRequest] -> return! json timeOffRequest next ctx
                | Ok _ -> return! Successful.NO_CONTENT next ctx
                | Error message ->
                    return! (BAD_REQUEST message) next ctx
            }

    let getUserBalance (identity: ServerTypes.Identity) (eventStore: IStore<UserId, RequestEvent>) =
        fun (next: HttpFunc) (ctx: HttpContext) ->
            let userId = identity.UserId

            let eventStream = eventStore.GetStream(userId)
            let state = eventStream.ReadAll() |> Seq.fold Logic.evolveUserRequests Map.empty
            let today = DateTime.Now

            task {
                let balance : UserVacationBalance = {
                    UserName = identity.UserId
                    BalanceYear = today.Year
                    PortionAccruedToDate = Logic.daysOffAdding today
                    CarriedOver = Logic.reportDaysOffAdding
                    TakenToDate = Logic.takenDaysOff today state
                    Planned = Logic.toComeDaysOff today state
                    CurrentBalance = Logic.daysOffSold today state
                }
                return! json balance next ctx
            }

// ---------------------------------
// Web app
// ---------------------------------

let webApp (eventStore: IStore<UserId, RequestEvent>) =
    let handleCommand (user: User) (command: Command) =
        let userId = command.UserId

        let eventStream = eventStore.GetStream(userId)
        let state = eventStream.ReadAll() |> Seq.fold Logic.evolveUserRequests Map.empty

        // Decide how to handle the command
        let result = Logic.decide DateTime.Now state user command

        // Save events in case of success
        match result with
        | Ok events -> eventStream.Append(events)
        | _ -> ()

        // Finally, return the result
        result
        
    choose [
        subRoute "/api"
            (choose [
                route "/users/login" >=> POST >=> Auth.login
                subRoute "/timeoff"
                    (Auth.requiresJwtTokenForAPI (fun identity ->
                        choose [
                            POST >=> route "/request" >=> HttpHandlers.requestTimeOff handleCommand identity
                            POST >=> route "/validate-request" >=> HttpHandlers.validateRequest handleCommand identity
                            GET >=> route "/user-balance" >=> HttpHandlers.getUserBalance identity eventStore
                        ]
                    ))
            ])
        setStatusCode 404 >=> text "Not Found" ]

// ---------------------------------
// Error handler
// ---------------------------------

let errorHandler (ex: Exception) (logger: ILogger) =
    logger.LogError(EventId(), ex, "An unhandled exception has occurred while executing the request.")
    clearResponse >=> setStatusCode 500 >=> text ex.Message

// ---------------------------------
// Config and Main
// ---------------------------------

let configureCors (builder: CorsPolicyBuilder) =
    builder.WithOrigins("http://localhost:8080")
           .AllowAnyMethod()
           .AllowAnyHeader()
           |> ignore

let configureApp (eventStore: IStore<UserId, RequestEvent>) (app: IApplicationBuilder) =
    let webApp = webApp eventStore
    let env = app.ApplicationServices.GetService<IHostingEnvironment>()
    (match env.IsDevelopment() with
    | true -> app.UseDeveloperExceptionPage()
    | false -> app.UseGiraffeErrorHandler errorHandler)
        .UseCors(configureCors)
        .UseStaticFiles()
        .UseGiraffe(webApp)

let configureServices (services: IServiceCollection) =
    services.AddCors() |> ignore
    services.AddGiraffe() |> ignore

let configureLogging (builder: ILoggingBuilder) =
    let filter (l: LogLevel) = l.Equals LogLevel.Error
    builder.AddFilter(filter).AddConsole().AddDebug() |> ignore

[<EntryPoint>]
let main _ =
    let contentRoot = Directory.GetCurrentDirectory()

    //let eventStore = InMemoryStore.Create<UserId, RequestEvent>()
    let storagePath = System.IO.Path.Combine(contentRoot, "../../../.storage", "userRequests")
    let eventStore = FileSystemStore.Create<UserId, RequestEvent>(storagePath, sprintf "%d")

    let webRoot = Path.Combine(contentRoot, "WebRoot")
    WebHostBuilder()
        .UseKestrel()
        .UseContentRoot(contentRoot)
        .UseIISIntegration()
        .UseWebRoot(webRoot)
        .Configure(Action<IApplicationBuilder>(configureApp eventStore))
        .ConfigureServices(configureServices)
        .ConfigureLogging(configureLogging)
        .Build()
        .Run()
    0
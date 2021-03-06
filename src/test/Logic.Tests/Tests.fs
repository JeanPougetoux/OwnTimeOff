module TimeOff.Tests

open Expecto
open System

let Given (events: RequestEvent list) = events
let ConnectedAs (user: User) (events: RequestEvent list) = events, user
let AndDateIs (year, month, day) (events: RequestEvent list, user: User) = events, user, DateTime(year, month, day)
let When (command: Command) (events: RequestEvent list, user: User, today: DateTime) = events, user, today, command
let Then expected message (events: RequestEvent list, user: User, today: DateTime, command: Command) =
    let evolveGlobalState (userStates: Map<UserId, Logic.UserRequestsState>) (event: RequestEvent) =
        let userState = defaultArg (Map.tryFind event.Request.UserId userStates) Map.empty
        let newUserState = Logic.evolveUserRequests userState event
        userStates.Add (event.Request.UserId, newUserState)

    let globalState = Seq.fold evolveGlobalState Map.empty events
    let userRequestsState = defaultArg (Map.tryFind command.UserId globalState) Map.empty
    let result = Logic.decide today userRequestsState user command
    Expect.equal result expected message

open System
open TimeOff
open TimeOff

[<Tests>]
let overlapTests = 
  testList "Overlap tests" [
    test "A request overlaps with itself" {
      let request = {
        UserId = 1
        RequestId = Guid.NewGuid()
        Start = { Date = DateTime(2018, 10, 1); HalfDay = AM }
        End = { Date = DateTime(2018, 10, 1); HalfDay = PM }
        Date = DateTime.Now
      }

      Expect.isTrue (Logic.overlapsWith request request) "A request should overlap with istself"
    }

    test "Requests on 2 distinct days don't overlap" {
      let request1 = {
        UserId = 1
        RequestId = Guid.NewGuid()
        Start = { Date = DateTime(2018, 10, 1); HalfDay = AM }
        End = { Date = DateTime(2018, 10, 1); HalfDay = PM }
        Date = DateTime.Now
      }

      let request2 = {
        UserId = 1
        RequestId = Guid.NewGuid()
        Start = { Date = DateTime(2018, 10, 2); HalfDay = AM }
        End = { Date = DateTime(2018, 10, 2); HalfDay = PM }
        Date = DateTime.Now
      }

      Expect.isFalse (Logic.overlapsWith request1 request2) "The requests don't overlap"
    }

    test "Requests when request 2 is in request 1 should overlap" {
      let request1 = {
        UserId = 1
        RequestId = Guid.NewGuid()
        Start = { Date = DateTime(2018, 10, 1); HalfDay = AM }
        End = { Date = DateTime(2018, 10, 5); HalfDay = PM }
        Date = DateTime.Now
      }

      let request2 = {
        UserId = 1
        RequestId = Guid.NewGuid()
        Start = { Date = DateTime(2018, 10, 2); HalfDay = AM }
        End = { Date = DateTime(2018, 10, 2); HalfDay = PM }
        Date = DateTime.Now
      }

      Expect.isTrue(Logic.overlapsWith request1 request2) "The requests overlap"
    }

    test "Request should overlap when any other overlap" {
      let request1 = {
        UserId = 1
        RequestId = Guid.NewGuid()
        Start = { Date = DateTime(2018, 10, 1); HalfDay = AM }
        End = { Date = DateTime(2018, 10, 5); HalfDay = PM }
        Date = DateTime.Now
      }

      let request2 = {
        UserId = 1
        RequestId = Guid.NewGuid()
        Start = { Date = DateTime(2018, 10, 2); HalfDay = AM }
        End = { Date = DateTime(2018, 10, 2); HalfDay = PM }
        Date = DateTime.Now
      }

      let mySeq = seq { yield request2 }
      Expect.isTrue(Logic.overlapsWithAnyRequest (mySeq) request1) "The requests overlap"
    }
  ]

[<Tests>]
let accountTests = 
  testList "Account Tests" [
    test "Calculate daysoff in febuary should result in 2.5 daysoff" {
      Expect.equal (Logic.daysOffAdding(DateTime(2018, 2, 2))) 2.5 "The number of daysoff does not match"
    }

    test "Calculate daysoff in november should result in 2.5 daysoff" {
      Expect.equal (Logic.daysOffAdding(DateTime(2018, 11, 11))) 25.0 "The number of daysoff does not match"
    }

    test "A request should have 106 days" {
      let request1 = {
        UserId = 1
        RequestId = Guid.NewGuid()
        Start = { Date = DateTime(2018, 09, 07); HalfDay = AM }
        End = { Date = DateTime(2018, 12, 21); HalfDay = PM }
        Date = DateTime.Now
      }
      Expect.equal (Logic.calculateRequestInDays request1) 106.0 "The request don't go for 106 days"
    }

    test "A request should have 105.5 days because AM" {
      let request1 = {
        UserId = 1
        RequestId = Guid.NewGuid()
        Start = { Date = DateTime(2018, 09, 07); HalfDay = AM }
        End = { Date = DateTime(2018, 12, 21); HalfDay = AM }
        Date = DateTime.Now
      }
      Expect.equal (Logic.calculateRequestInDays request1) 105.5 "The request don't go for 105.5 days"
    }

    test "A request should have 105.5 days because PM" {
      let request1 = {
        UserId = 1
        RequestId = Guid.NewGuid()
        Start = { Date = DateTime(2018, 09, 07); HalfDay = PM }
        End = { Date = DateTime(2018, 12, 21); HalfDay = PM }
        Date = DateTime.Now
      }
      Expect.equal (Logic.calculateRequestInDays request1) 105.5 "The request don't go for 105.5 days"
    }

    test "Multiple active back requests testing - 129 days" {
      let request1 = {
        UserId = 1
        RequestId = Guid.NewGuid()
        Start = { Date = DateTime(2018, 09, 07); HalfDay = AM }
        End = { Date = DateTime(2018, 12, 21); HalfDay = PM }
        Date = DateTime.Now
      } // 106
      let request2 = {
        UserId = 1
        RequestId = Guid.NewGuid()
        Start = { Date = DateTime(2018, 12, 21); HalfDay = AM }
        End = { Date = DateTime(2018, 12, 26); HalfDay = PM }
        Date = DateTime.Now
      } // 6
      let request3 = {
        UserId = 1
        RequestId = Guid.NewGuid()
        Start = { Date = DateTime(2018, 08, 15); HalfDay = AM }
        End = { Date = DateTime(2018, 08, 31); HalfDay = PM }
        Date = DateTime.Now
      } // 17
      let request4 = {
        UserId = 1
        RequestId = Guid.NewGuid()
        Start = { Date = DateTime(2018, 08, 3); HalfDay = AM }
        End = { Date = DateTime(2018, 08, 4); HalfDay = PM }
        Date = DateTime.Now
      } // inactive
      let request5 = {
        UserId = 1
        RequestId = Guid.NewGuid()
        Start = { Date = DateTime(2017, 08, 3); HalfDay = AM }
        End = { Date = DateTime(2017, 08, 4); HalfDay = PM }
        Date = DateTime.Now
      } // in 2017
      let request6 = {
        UserId = 1
        RequestId = Guid.NewGuid()
        Start = { Date = DateTime(2019, 08, 3); HalfDay = AM }
        End = { Date = DateTime(2019, 08, 4); HalfDay = PM }
        Date = DateTime.Now
      } // future
      let ev request =
        Logic.evolveRequest null (RequestValidated request)

      let inactev request =
        Logic.evolveRequest null (RequestCancelled request)

      let map = Map.empty.Add(request1.RequestId, ev request1).Add(request2.RequestId, ev request2).Add(request3.RequestId, ev request3).Add(request4.RequestId, inactev request4).Add(request5.RequestId, ev request5).Add(request6.RequestId, ev request6)
      let numbDays = Logic.takenDaysOff (DateTime(2018, 12, 31)) map

      Expect.equal (Logic.calculateRequestInDays request1) 106.0 "The request don't go for 106 days"
      Expect.equal (Logic.calculateRequestInDays request2) 6.0 "The request don't go for 6 days"
      Expect.equal (Logic.calculateRequestInDays request3) 17.0 "The request don't go for 17 days"
      Expect.equal numbDays 129.0 "The requests don't go for 129 days"
    }

    test "Multiple active future requests testing - 129 days" {
      let request1 = {
        UserId = 1
        RequestId = Guid.NewGuid()
        Start = { Date = DateTime(2018, 09, 07); HalfDay = AM }
        End = { Date = DateTime(2018, 12, 21); HalfDay = PM }
        Date = DateTime.Now
      } // 106
      let request2 = {
        UserId = 1
        RequestId = Guid.NewGuid()
        Start = { Date = DateTime(2018, 12, 21); HalfDay = AM }
        End = { Date = DateTime(2018, 12, 26); HalfDay = PM }
        Date = DateTime.Now
      } // 6
      let request3 = {
        UserId = 1
        RequestId = Guid.NewGuid()
        Start = { Date = DateTime(2018, 08, 15); HalfDay = AM }
        End = { Date = DateTime(2018, 08, 31); HalfDay = PM }
        Date = DateTime.Now
      } // 17
      let request4 = {
        UserId = 1
        RequestId = Guid.NewGuid()
        Start = { Date = DateTime(2018, 08, 3); HalfDay = AM }
        End = { Date = DateTime(2018, 08, 4); HalfDay = PM }
        Date = DateTime.Now
      } // inactive
      let request5 = {
        UserId = 1
        RequestId = Guid.NewGuid()
        Start = { Date = DateTime(2019, 08, 3); HalfDay = AM }
        End = { Date = DateTime(2019, 08, 4); HalfDay = PM }
        Date = DateTime.Now
      } // in 2019
      let request6 = {
        UserId = 1
        RequestId = Guid.NewGuid()
        Start = { Date = DateTime(2018, 01, 3); HalfDay = AM }
        End = { Date = DateTime(2018, 01, 7); HalfDay = PM }
        Date = DateTime.Now
      } // past
      let ev request =
        Logic.evolveRequest null (RequestValidated request)

      let inactev request =
        Logic.evolveRequest null (RequestCancelled request)

      let map = Map.empty.Add(request1.RequestId, ev request1).Add(request2.RequestId, ev request2).Add(request3.RequestId, ev request3).Add(request4.RequestId, inactev request4).Add(request5.RequestId, ev request5).Add(request6.RequestId, ev request6)
      let numbDays = Logic.toComeDaysOff (DateTime(2018, 03, 03)) map

      Expect.equal (Logic.calculateRequestInDays request1) 106.0 "The request don't go for 106 days"
      Expect.equal (Logic.calculateRequestInDays request2) 6.0 "The request don't go for 6 days"
      Expect.equal (Logic.calculateRequestInDays request3) 17.0 "The request don't go for 17 days"
      Expect.equal numbDays 129.0 "The requests don't go for 129 days"
    }

    test "Multiple requests testing - 129 days" {
      let reported = Logic.reportDaysOffAdding
      let request1 = {
        UserId = 1
        RequestId = Guid.NewGuid()
        Start = { Date = DateTime(2018, 09, 07); HalfDay = AM }
        End = { Date = DateTime(2018, 09, 11); HalfDay = PM }
        Date = DateTime.Now
      } // 5
      let request2 = {
        UserId = 1
        RequestId = Guid.NewGuid()
        Start = { Date = DateTime(2018, 12, 21); HalfDay = AM }
        End = { Date = DateTime(2018, 12, 26); HalfDay = PM }
        Date = DateTime.Now
      } // 6
      let request3 = {
        UserId = 1
        RequestId = Guid.NewGuid()
        Start = { Date = DateTime(2018, 08, 15); HalfDay = AM }
        End = { Date = DateTime(2018, 08, 19); HalfDay = PM }
        Date = DateTime.Now
      } // 5
      let request4 = {
        UserId = 1
        RequestId = Guid.NewGuid()
        Start = { Date = DateTime(2018, 08, 3); HalfDay = AM }
        End = { Date = DateTime(2018, 08, 4); HalfDay = PM }
        Date = DateTime.Now
      } // inactive
      let request5 = {
        UserId = 1
        RequestId = Guid.NewGuid()
        Start = { Date = DateTime(2017, 08, 3); HalfDay = AM }
        End = { Date = DateTime(2017, 08, 4); HalfDay = PM }
        Date = DateTime.Now
      } // in 2017
      let request6 = {
        UserId = 1
        RequestId = Guid.NewGuid()
        Start = { Date = DateTime(2019, 08, 3); HalfDay = AM }
        End = { Date = DateTime(2019, 08, 4); HalfDay = PM }
        Date = DateTime.Now
      } // in 2019
      let ev request =
        Logic.evolveRequest null (RequestValidated request)

      let inactev request =
        Logic.evolveRequest null (RequestCancelled request)

      let map = Map.empty.Add(request1.RequestId, ev request1).Add(request2.RequestId, ev request2).Add(request3.RequestId, ev request3).Add(request4.RequestId, inactev request4).Add(request5.RequestId, ev request5).Add(request6.RequestId, ev request6)
      let numbDays = Logic.daysOffSold (DateTime(2018, 09, 01)) map

      Expect.equal (Logic.calculateRequestInDays request1) 5.0 "The request don't go for 5 days"
      Expect.equal (Logic.calculateRequestInDays request2) 6.0 "The request don't go for 6 days"
      Expect.equal (Logic.calculateRequestInDays request3) 5.0 "The request don't go for 5 days"
      Expect.equal (numbDays - reported) 4.0 "The total at end is not 20 - 16 = 4"
    }
  ]

[<Tests>]
let creationTests =
  testList "Creation tests" [
    test "A request is created" {
      let request = {
        UserId = 1
        RequestId = Guid.NewGuid()
        Start = { Date = DateTime(2018, 12, 28); HalfDay = AM }
        End = { Date = DateTime(2018, 12, 28); HalfDay = PM }
        Date = DateTime(2018, 12, 3)
      }

      Given [ ]
      |> ConnectedAs (Employee 1)
      |> AndDateIs (2018, 12, 3)
      |> When (RequestTimeOff request)
      |> Then (Ok [RequestCreated request]) "The request should have been created"
    }

    test "A request with end before start should be rejected" {
      let request = {
        UserId = 1
        RequestId = Guid.NewGuid()
        Start = { Date = DateTime(2018, 12, 29); HalfDay = AM }
        End = { Date = DateTime(2018, 12, 28); HalfDay = PM }
        Date = DateTime(2018, 12, 3)
      }

      Given [ ]
      |> ConnectedAs (Employee 1)
      |> AndDateIs (2018, 12, 3)
      |> When (RequestTimeOff request)
      |> Then (Error "The request end is before the start") "The request should not have been created"
    }

    test "A request in the past cannot be created - version system date" {
      let request = {
        UserId = 1
        RequestId = Guid.NewGuid()
        Start = { Date = DateTime(2018, 11, 28); HalfDay = AM }
        End = { Date = DateTime(2018, 11, 28); HalfDay = PM } 
        Date = DateTime.Now
        }

      Given [ ]
      |> ConnectedAs (Employee 1)
      |> AndDateIs (2018, 12, 3)
      |> When (RequestTimeOff request)
      |> Then (Error "The request starts in the past") "The request should not have been created"
    }

    test "A request in the past cannot be created - version request date" {
      let request = {
        UserId = 1
        RequestId = Guid.NewGuid()
        Start = { Date = DateTime(2018, 11, 28); HalfDay = AM }
        End = { Date = DateTime(2018, 11, 28); HalfDay = PM } 
        Date = DateTime(2018, 12, 3)
        }

      Given [ ]
      |> ConnectedAs (Employee 1)
      |> AndDateIs (2018, 11, 3)
      |> When (RequestTimeOff request)
      |> Then (Error "The request starts in the past") "The request should not have been created"
    }

    test "A request on the same day cannot be created - version system date" {
      let request = {
        UserId = 1
        RequestId = Guid.NewGuid()
        Start = { Date = DateTime(2018, 11, 28); HalfDay = PM }
        End = { Date = DateTime(2018, 11, 29); HalfDay = PM } 
        Date = DateTime.Now
        }

      Given [ ]
      |> ConnectedAs (Employee 1)
      |> AndDateIs (2018, 11, 28)
      |> When (RequestTimeOff request)
      |> Then (Error "The request starts in the past") "The request should not have been created"
    }

    test "A request on the same day cannot be created - version request date" {
      let request = {
        UserId = 1
        RequestId = Guid.NewGuid()
        Start = { Date = DateTime(2018, 11, 28); HalfDay = PM }
        End = { Date = DateTime(2018, 11, 29); HalfDay = PM } 
        Date = DateTime(2018, 11, 28)
        }

      Given [ ]
      |> ConnectedAs (Employee 1)
      |> AndDateIs (2018, 11, 3)
      |> When (RequestTimeOff request)
      |> Then (Error "The request starts in the past") "The request should not have been created"
    }

    test "Two request with same id cannot be created" {
      let request1 = {
        UserId = 1
        RequestId = new Guid("4b8d6dea-3eab-4f1a-97c2-879e479f1555")
        Start = { Date = DateTime(2018, 12, 28); HalfDay = AM }
        End = { Date = DateTime(2018, 12, 28); HalfDay = PM } 
        Date = DateTime.Now
        }
       
      let request2 = {
        UserId = 1
        RequestId = new Guid("4b8d6dea-3eab-4f1a-97c2-879e479f1555")
        Start = { Date = DateTime(2018, 12, 29); HalfDay = AM }
        End = { Date = DateTime(2018, 12, 29); HalfDay = PM } 
        Date = DateTime.Now
        }

      Given [ RequestCreated request1 ]
      |> ConnectedAs (Employee 1)
      |> AndDateIs (2018, 12, 3)
      |> When (RequestTimeOff request2)
      |> Then (Error "A request has already the same guid") "The request should not have been created"
    }

    test "Request in january take too much days that the user do not have" {
      let request1 = {
        UserId = 1
        RequestId = new Guid("4b8d6dea-3eab-4f1a-97c2-879e479f1555")
        Start = { Date = DateTime(2019, 02, 01); HalfDay = AM }
        End = { Date = DateTime(2019, 02, 25); HalfDay = PM } 
        Date = DateTime.Now
        }

      Given [ ]
      |> ConnectedAs (Employee 1)
      |> AndDateIs (2019, 01, 11)
      |> When (RequestTimeOff request1)
      |> Then (Error "You have not enough days to proceed the request") "The request should not have been created"
    }
  ]

[<Tests>]
let validationTests =
  testList "Validation tests" [
    test "A request is validated" {
      let request = {
        UserId = 1
        RequestId = Guid.Empty
        Start = { Date = DateTime(2018, 12, 28); HalfDay = AM }
        End = { Date = DateTime(2018, 12, 28); HalfDay = PM } 
        Date = DateTime.Now
        }

      Given [ RequestCreated request ]
      |> ConnectedAs Manager
      |> AndDateIs (2018, 12, 3)
      |> When (ValidateRequest (1, request.RequestId))
      |> Then (Ok [RequestValidated request]) "The request should have been validated"
    }

    test "A cancelled request can't be validated" {
      let request = {
        UserId = 1
        RequestId = Guid.Empty
        Start = { Date = DateTime(2018, 12, 28); HalfDay = AM }
        End = { Date = DateTime(2018, 12, 28); HalfDay = PM } 
        Date = DateTime.Now
        }

      Given [ RequestCreated request; RequestCancelled request ]
      |> ConnectedAs Manager
      |> AndDateIs (2018, 12, 3)
      |> When (ValidateRequest (1, request.RequestId))
      |> Then (Error "Request cannot be validated") "The validation of the request should have been refused"
    }

    test "An already validated request can't be validated" {
      let request = {
        UserId = 1
        RequestId = Guid.Empty
        Start = { Date = DateTime(2018, 12, 28); HalfDay = AM }
        End = { Date = DateTime(2018, 12, 28); HalfDay = PM } 
        Date = DateTime.Now
        }

      Given [ RequestCreated request; RequestValidated request ]
      |> ConnectedAs Manager
      |> AndDateIs (2018, 12, 3)
      |> When (ValidateRequest (1, request.RequestId))
      |> Then (Error "Request cannot be validated") "The validation of the request should have been refused"
    }

    test "A request is accepted to cancel by a manager" {
      let request = {
        UserId = 1
        RequestId = Guid.Empty
        Start = { Date = DateTime(2018, 12, 28); HalfDay = AM }
        End = { Date = DateTime(2018, 12, 28); HalfDay = PM } 
        Date = DateTime.Now
        }

      Given [ RequestCreated request; RequestAskedToCancel request ]
      |> ConnectedAs (Manager)
      |> AndDateIs (2018, 12, 4)
      |> When (AcceptCancelationRequest (1, request.RequestId))
      |> Then (Ok [RequestCancelled request]) "The request should have been cancelled"
    }

    test "A cancelled request can't be accepted to cancel by a manager" {
      let request = {
        UserId = 1
        RequestId = Guid.Empty
        Start = { Date = DateTime(2018, 12, 28); HalfDay = AM }
        End = { Date = DateTime(2018, 12, 28); HalfDay = PM } 
        Date = DateTime.Now
        }

      Given [ RequestCreated request; RequestCancelled request ]
      |> ConnectedAs (Manager)
      |> AndDateIs (2018, 12, 4)
      |> When (AcceptCancelationRequest (1, request.RequestId))
      |> Then (Error "The request should be pending cancelling") "The acceptation of the cancel of the request should have been refused"
    }

    test "A validated request can't be accepted to cancel by a manager" {
      let request = {
        UserId = 1
        RequestId = Guid.Empty
        Start = { Date = DateTime(2018, 12, 28); HalfDay = AM }
        End = { Date = DateTime(2018, 12, 28); HalfDay = PM } 
        Date = DateTime.Now
        }

      Given [ RequestCreated request; RequestValidated request ]
      |> ConnectedAs (Manager)
      |> AndDateIs (2018, 12, 4)
      |> When (AcceptCancelationRequest (1, request.RequestId))
      |> Then (Error "The request should be pending cancelling") "The acceptation of the cancel of the request should have been refused"
    }
  ]

[<Tests>]
let unvalidationTests =
  testList "Refusing tests" [
    test "A request is refused" {
      let request = {
        UserId = 1
        RequestId = Guid.Empty
        Start = { Date = DateTime(2018, 12, 28); HalfDay = AM }
        End = { Date = DateTime(2018, 12, 28); HalfDay = PM } 
        Date = DateTime.Now
        }

      Given [ RequestCreated request ]
      |> ConnectedAs Manager
      |> AndDateIs (2018, 12, 3)
      |> When (RefuseRequest (1, request.RequestId))
      |> Then (Ok [RequestRefused request]) "The request should have been cancelled"
    }

    test "A cancelled request can't be refused" {
      let request = {
        UserId = 1
        RequestId = Guid.Empty
        Start = { Date = DateTime(2018, 12, 28); HalfDay = AM }
        End = { Date = DateTime(2018, 12, 28); HalfDay = PM } 
        Date = DateTime.Now
        }

      Given [ RequestCreated request; RequestCancelled request ]
      |> ConnectedAs Manager
      |> AndDateIs (2018, 12, 3)
      |> When (RefuseRequest (1, request.RequestId))
      |> Then (Error "Request cannot be refused") "The refusing of the request should have been refused"
    }

    test "An already validated request can't be refused" {
      let request = {
        UserId = 1
        RequestId = Guid.Empty
        Start = { Date = DateTime(2018, 12, 28); HalfDay = AM }
        End = { Date = DateTime(2018, 12, 28); HalfDay = PM } 
        Date = DateTime.Now
        }

      Given [ RequestCreated request; RequestValidated request ]
      |> ConnectedAs Manager
      |> AndDateIs (2018, 12, 3)
      |> When (RefuseRequest (1, request.RequestId))
      |> Then (Error "Request cannot be refused") "The validation of the request should have been refused"
    }

    test "A request is refused to cancel by a manager" {
      let request = {
        UserId = 1
        RequestId = Guid.Empty
        Start = { Date = DateTime(2018, 12, 28); HalfDay = AM }
        End = { Date = DateTime(2018, 12, 28); HalfDay = PM } 
        Date = DateTime.Now
        }

      Given [ RequestCreated request; RequestAskedToCancel request ]
      |> ConnectedAs (Manager)
      |> AndDateIs (2018, 12, 4)
      |> When (RefuseCancelationRequest (1, request.RequestId))
      |> Then (Ok [RequestValidated request]) "The request should have been validated again"
    }

    test "A cancelled request can't be refused to cancel by a manager" {
      let request = {
        UserId = 1
        RequestId = Guid.Empty
        Start = { Date = DateTime(2018, 12, 28); HalfDay = AM }
        End = { Date = DateTime(2018, 12, 28); HalfDay = PM } 
        Date = DateTime.Now
        }

      Given [ RequestCreated request; RequestCancelled request ]
      |> ConnectedAs (Manager)
      |> AndDateIs (2018, 12, 4)
      |> When (RefuseCancelationRequest (1, request.RequestId))
      |> Then (Error "The request should be pending cancelling") "The acceptation of the cancel of the request should have been refused"
    }

    test "A validated request can't be refused to cancel by a manager" {
      let request = {
        UserId = 1
        RequestId = Guid.Empty
        Start = { Date = DateTime(2018, 12, 28); HalfDay = AM }
        End = { Date = DateTime(2018, 12, 28); HalfDay = PM } 
        Date = DateTime.Now
        }

      Given [ RequestCreated request; RequestValidated request ]
      |> ConnectedAs (Manager)
      |> AndDateIs (2018, 12, 4)
      |> When (RefuseCancelationRequest (1, request.RequestId))
      |> Then (Error "The request should be pending cancelling") "The acceptation of the cancel of the request should have been refused"
    }
  ]

[<Tests>]
let cancellingTests =
  testList "Cancelling tests" [
    test "An inactive request can't be cancelled by user" {
      let request = {
        UserId = 1
        RequestId = Guid.Empty
        Start = { Date = DateTime(2018, 12, 28); HalfDay = AM }
        End = { Date = DateTime(2018, 12, 28); HalfDay = PM } 
        Date = DateTime.Now
        }

      Given [ RequestCreated request; RequestCancelled request ]
      |> ConnectedAs (Employee 1)
      |> AndDateIs (2018, 12, 4)
      |> When (CancelRequest (request.UserId, request.RequestId))
      |> Then (Error "The request is not active") "The cancel of the request should have been refused"
    }

    test "An inactive request can't be cancelled by manager" {
      let request = {
        UserId = 1
        RequestId = Guid.Empty
        Start = { Date = DateTime(2018, 12, 28); HalfDay = AM }
        End = { Date = DateTime(2018, 12, 28); HalfDay = PM } 
        Date = DateTime.Now
        }

      Given [ RequestCreated request; RequestCancelled request ]
      |> ConnectedAs (Manager)
      |> AndDateIs (2018, 12, 4)
      |> When (CancelRequest (request.UserId, request.RequestId))
      |> Then (Error "The request is not active") "The cancel of the request should have been refused"
    }

    test "A request in the future is cancelled by the user" {
      let request = {
        UserId = 1
        RequestId = Guid.Empty
        Start = { Date = DateTime(2018, 12, 28); HalfDay = AM }
        End = { Date = DateTime(2018, 12, 28); HalfDay = PM } 
        Date = DateTime.Now
        }

      Given [ RequestCreated request ]
      |> ConnectedAs (Employee 1)
      |> AndDateIs (2018, 12, 4)
      |> When (CancelRequest (request.UserId, request.RequestId))
      |> Then (Ok [RequestCancelled request]) "The request should have been cancelled"
    }

    test "A request in the past is asked to cancel by the user" {
      let request = {
        UserId = 1
        RequestId = Guid.Empty
        Start = { Date = DateTime(2018, 12, 28); HalfDay = AM }
        End = { Date = DateTime(2018, 12, 30); HalfDay = PM } 
        Date = DateTime.Now
        }

      Given [ RequestCreated request ]
      |> ConnectedAs (Employee 1)
      |> AndDateIs (2018, 12, 29)
      |> When (CancelRequest (request.UserId, request.RequestId))
      |> Then (Ok [RequestAskedToCancel request]) "The request has been asked to cancel"
    }

    test "A request is cancelled by the manager" {
      let request = {
        UserId = 1
        RequestId = Guid.Empty
        Start = { Date = DateTime(2018, 12, 28); HalfDay = AM }
        End = { Date = DateTime(2018, 12, 28); HalfDay = PM } 
        Date = DateTime.Now
        }

      Given [ RequestCreated request ]
      |> ConnectedAs (Manager)
      |> AndDateIs (2018, 12, 4)
      |> When (CancelRequest (request.UserId, request.RequestId))
      |> Then (Ok [RequestCancelled request]) "The request should have been cancelled"
    }
  ]
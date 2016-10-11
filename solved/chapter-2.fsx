(*
CHAPTER 2
+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

We have data, we have charts - let's make predictions now.
We will create a very crude model, a "decision stump", and
see where we could then take it from there, and improve
our predictions.
*)


(*
What we have so far
+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
*)

#r "../packages/fsharp.data/lib/net40/fsharp.data.dll"
open FSharp.Data

#r "../packages/newtonsoft.json/lib/net45/newtonsoft.json.dll"
#r "../packages/xplot.googlecharts/lib/net45/xplot.googlecharts.dll"
#r "../packages/google.datatable.net.wrapper/lib/google.datatable.net.wrapper.dll"
open XPlot.GoogleCharts

[<Literal>]
let redWinesPath = @"../data/winequality-red.csv"

type Wines = 
    CsvProvider<
        Sample = redWinesPath,
        Separators = ";",
        Schema = "float,float,float,float,float,float,float,float,float,float,float,float">

type Wine = Wines.Row

let redWines = Wines.GetSample().Rows

let options = Configuration.Options()
options.dataOpacity <- 0.20
options.pointSize <- 10



(*
TUTORIAL: F# functions
+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
If you are familiar with this, you can skip to next topic. 

F# being "functional first", functions are a key element of
the language. 
*)

let addOne x = x + 1

addOne 42

42 |> addOne

let isGreaterThan42 x =
    let z = 42
    if x > z
    then "TRUE"
    else "FALSE"

isGreaterThan42 43

let add x y =
    x + y

add 1 2

2 |> add 1 |> add 1 |> add 1

let add42 = add 42

add42 1

let isBetween (low,high) x =
    (x <= high && x >= low)

isBetween (10,20) 30

42 |> isBetween (40,43)



(*
Step 1: building a stump
+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

Our goal is to create a "predictor": given a wine, we want
to return a value, the predicted Quality, using some of the
information we have available about that wine.
A Predictor is a function: Wine -> float.
The predictor we will start with is called a Stump. It is a
gate: if some input value is under a threshold, predict a
value, otherwise, predict another value.
*)

// a very basic predictor : "always 5.0"
let veryBasicPredictor (wine:Wine) = 5.0

// a stump: "if sugar < level, predict 4, else predict 6"
let sugarStump sugarLevel (wine:Wine) =
    if wine.``Residual sugar`` < sugarLevel
    then 4.0
    else 6.0

redWines |> Seq.item 0 |> sugarStump 2.0
redWines |> Seq.item 1 |> sugarStump 2.0

// [TODO] write a more general stump for alcohol level.
// if the wine alcohol level is below the given level, 
// predict the average quality of wines below that level,
// otherwise predict average quality of wines above.

let learnAlcoholStump alcoholLevel =
    // average quality for wines with alcohol <= level
    let valueIfLow = 
        redWines
        |> Seq.filter (fun wine -> wine.Alcohol <= alcoholLevel)
        |> Seq.averageBy (fun wine -> wine.Quality)
    // average quality for wines with alcohol > level
    let valueIfHigh =
        redWines
        |> Seq.filter (fun wine -> wine.Alcohol > alcoholLevel)
        |> Seq.averageBy (fun wine -> wine.Quality)
    // create a stump
    let predictor (wine:Wine) =
        if wine.Alcohol <= alcoholLevel
        then valueIfLow
        else valueIfHigh
    // return the stump
    predictor


(*
Step 2: trying out the alcohol stump
+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

Let's take a look at how the stump works.
*)

// we will try 2 "reasonable" alcohol levels, between the
// minimum and maximum on our sample 
let minAlcohol = 
    redWines 
    |> Seq.map (fun wine -> wine.Alcohol) 
    |> Seq.min
let maxAlcohol = 
    redWines 
    |> Seq.map (fun wine -> wine.Alcohol) 
    |> Seq.max

// [TODO] alcohol level is between 8.4 and 14.9; let's try 
// to learn stumps for alcohol level s of 10.0 and 12.0.

let alcoholStump1 = learnAlcoholStump 10.0
let alcoholStump2 = learnAlcoholStump 12.0


// See what's happening on a plot 

let actuals = 
    redWines 
    |> Seq.map (fun wine -> wine.Alcohol,wine.Quality)
let predictions1 = 
    redWines 
    |> Seq.map (fun wine -> wine.Alcohol, alcoholStump1 wine)
let predictions2 = 
    redWines 
    |> Seq.map (fun wine -> wine.Alcohol, alcoholStump2 wine)

[ actuals; predictions1; predictions2 ]
|> Chart.Scatter
|> Chart.Show



(*
Step 3: Comparing Models
+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

Now that we have a few predictors, how do we pick one? To 
do this, we need a way to compare them. We would like one
number to measure how good or bad a predictor is.
We will create a cost function: a cost of 0 means "every
prediction is correct", higher cost indicates more errors,
that is, differences between actuals and predicted.
*)

// let's consider our veryBasicPredictor
// let veryBasicPredictor (wine:Wine) = 5.0
// take a look at predictions and "error"
redWines
|> Seq.take 20
|> Seq.iter (fun wine ->
    let quality = wine.Quality
    let predicted = veryBasicPredictor wine
    let error = quality - predicted
    printfn "Qual: %.1f Pred: %.1f Err: %.1f" quality predicted error
    )

// comments:
// a perfect model would have error = 0 everywhere
// the prediction can be over or under
// what is bad is a large difference in either direction.

// a common approach is to penalize errors with squares:
// 0 error is 0, larger errors get increasing penalties
let pownInAction = pown (-4.0) 2

redWines
|> Seq.take 20
|> Seq.iter (fun wine ->
    let quality = wine.Quality
    let predicted = veryBasicPredictor wine
    let cost = pown (quality - predicted) 2
    printfn "Qual: %.1f Pred: %.1f Cost: %.1f" quality predicted cost
    )

// We can now measure how good or bad a model is,
// by computing the sum, or the average cost.

// [TODO] compute the average cost of:
// veryBasicPredictor
// let model1 = learnAlcoholStump 10.0
// let model2 = learnAlcoholStump 12.0
// hint: Seq.averageBy could be useful
let cost0 = 
    redWines 
    |> Seq.averageBy (fun wine -> 
        pown ((wine.Quality)-(veryBasicPredictor wine)) 2)

let cost1 = 
    redWines 
    |> Seq.averageBy (fun wine -> 
        pown ((wine.Quality)-(alcoholStump1 wine)) 2)

let cost2 = 
    redWines 
    |> Seq.averageBy (fun wine -> 
        pown ((wine.Quality)-(alcoholStump2 wine)) 2)

// what is our best model so far?



(*
Step 4: Finding the best stump 
+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

All we need now is to find the alcohol level that gives us
the stump with lowest cost. We will create a "grid" of
possible values between the min and max, and pick the best. 
*)

let levels =
    let values = 
        redWines
        |> Seq.map (fun wine -> wine.Alcohol)    
    let min = values |> Seq.min
    let max = values |> Seq.max
    let width = max - min
    let step = width / 20.0
    [ min + step .. step .. max - step ]

// [TODO] find the alcohol stump with best cost
let bestStump =
    levels
    |> Seq.map (fun level -> learnAlcoholStump level)
    |> Seq.minBy (fun stump -> 
        redWines 
        |> Seq.averageBy (fun wine -> 
            pown ((wine.Quality)-(stump wine)) 2))

let bestCost = 
    redWines 
    |> Seq.averageBy (fun wine -> 
        pown ((wine.Quality)-(bestStump wine)) 2)



(*
Step 5: Plot the residuals
+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

This model is obviously pretty weak. Can we do better?
One way to get better is to figure out what we didn't do
great. In this case, this means inspecting the predictions,
and looking at where they differ from the "correct" value.
This is called the Residuals.
*)

// let's look first at actuals vs. predictions
redWines
|> Seq.map (fun wine -> wine.Quality, bestStump wine)
|> Chart.Scatter
|> Chart.WithOptions options
|> Chart.WithTitle "Actual vs. Predicted"
|> Chart.WithXTitle "Quality"
|> Chart.WithYTitle "Predicted"
|> Chart.Show

// now let's look at the residuals
redWines
|> Seq.map (fun wine -> 
    wine.Alcohol, wine.Quality - bestStump wine)
|> Chart.Scatter
|> Chart.WithOptions options
|> Chart.WithTitle "Alcohol Level vs. Error"
|> Chart.WithXTitle "Alcohol Level"
|> Chart.WithYTitle "Error"
|> Chart.Show

(*
The residuals are not "evenly distributed".
For alcohol levels under ~10.5, we seem to over-shoot,
whereas in the 10.5-11.5 range, we under-predict.
What we could try is find a new stump, trying to predict
against the residuals, to correct our errors, and 
combine it with the original stump.
This is what we will do next.
*)
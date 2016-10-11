(*
CHAPTER 3
+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

We can now learn a "weak" predictor, a Stump: based on a
single measurement, we predict either a low value if the
Wine is below a certain level, or a high value if it is
above.
To refine our prediction model, we will do 2 things next:
1) once we have a model (a stump, or something else), we 
will look at the predictions, and try to find another stump
to correct what we didn't predict well,
2) instead of using a single measurement, we will try to
use all the measurements we have available.
*)

(*
What we have so far
+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
*)

#I "../packages/"
#r "fsharp.data/lib/net40/fsharp.data.dll"
open FSharp.Data

#r "newtonsoft.json/lib/net45/newtonsoft.json.dll"
#r "xplot.googlecharts/lib/net45/xplot.googlecharts.dll"
#r "google.datatable.net.wrapper/lib/google.datatable.net.wrapper.dll"
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
options.dataOpacity <- 0.10
options.pointSize <- 10


(*
Step1: Introducing abstractions
+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

So far we have used Alcohol level to predict Quality.
Now, we want to use any measurement, and predict Quality,
but also possibly residuals, that is, any number we please.

Let's use a couple of type aliases to clarify the model.
*)

// an Observation is a Wine
type Observation = Wines.Row
// an Example is a tuple: Wine and the value we want to 
// predict (Quality or Residuals - or whatever)
type Example = Observation * float
// a Feature is a value we extract from a Wine
type Feature = Observation -> float
// a Predictor is a function that predicts a value for a Wine
type Predictor = Observation -> float


(*
Step 2: Using abstractions
+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
*)

// we can now define "Features"
let alcohol (wine:Observation) = wine.Alcohol
// or - also possible
let chlorides : Feature = 
    fun wine -> wine.Chlorides

// We can also learn a stump for any arbitrary feature.
let learnStump 
    (sample:Example seq) 
        (feature:Feature) 
            (level:float) =
    // compute min and max value for the feature, across
    // our sample of examples.
    let low = 
        sample
        |> Seq.filter (fun (wine,value) -> 
            feature wine <= level)
        |> Seq.averageBy (fun (wine,value) -> value)
    let high =
        sample
        |> Seq.filter (fun (wine,value) -> 
            feature wine > level)
        |> Seq.averageBy (fun (wine,value) -> value)
    // create a Predictor function...
    let predictor (wine:Observation) =
        if feature wine <= level
        then low
        else high
    // ... and return it.
    predictor

// we can similarly rewrite the cost function
let cost (sample:Example seq) (predictor:Predictor) = 
    sample
    |> Seq.averageBy (fun (wine,value) -> 
        pown (predictor wine - value) 2)


// [TODO] / illustration
// Example: learn a stump that predicts quality, based on
// whether the alcohol level is above or below 11.5, and
// compute the cost of that model.
let ``alcohol over/under 11.5`` =
    // we prepare the sample: what is the observation,
    // and what are we trying to predict.
    let sample = 
        redWines
        |> Seq.map (fun wine -> wine,wine.Quality)
    // we learn, using a feature and a level
    // [TODO] learn stump for alcohol, level 11.5
    let stump = learnStump sample alcohol 11.5
    // we compute the cost
    // [TODO] compute the stump cost
    printfn "Quality of model: %.3f" (cost sample stump) 


// [TODO] Re-write the levels function: given a sample of
// examples, for a Feature, find (n-1) evenly spaced values
// that define n segments of even length.
let levels (sample:Example seq) (feature:Feature) (n:int) =
    let featureValues = 
        sample 
        |> Seq.map (fun (wine,value) -> feature wine)
    let min = featureValues |> Seq.min
    let max = featureValues |> Seq.max
    let width = max - min
    let step = width / (float n)
    [ min + step.. step .. max - step ]



(*
Step 3: Learning recursively
+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

We have all the pieces we need to write our algorithm:
- given a Sample and a Predictor,
- compute the Residuals, the difference between the 
predicted value and the actual value,
- learn a Stump to predict the Residuals,
- create a new Predictor <- Predictor + Stump,
- repeat until a certain depth is reached.
*)
    

// [TODO] Fix the learn function where indicated
let learn 
    (sample:Example seq) 
        (features:Feature seq) 
            (grid:int) 
                (depth:int) =

    // for each feature, pre-compute possible split values
    // into a sequence of every possible (Feature * Level)
    let featuresLevels =
        features
        |> Seq.collect (fun feature -> 
            let levels = levels sample feature grid
            levels 
            |> List.map (fun level -> feature,level))

    // main recursive search / learning algorithm
    let rec search (predictor:Predictor) (currDepth:int) =
        // if we reach maximum depth, 
        if currDepth >= depth
        // we are done and return the current Predictor
        then predictor
        else
            // [TODO] Compute the residuals
            let residualsSample = 
                sample
                |> Seq.map (fun (wine,quality) -> 
                    wine,
                    quality - predictor wine)
            // [TODO] Learn a Stump for each feature level,
            // and find the stump with the best cost.
            let bestResidualStump =
                featuresLevels
                |> Seq.map (fun (feature,level) -> 
                    learnStump residualsSample feature level)
                |> Seq.minBy (cost residualsSample)
            // We combine old predictor with the best stump,   
            let bestPredictor (wine:Observation) = 
                predictor wine + bestResidualStump wine
            // and we search one level deeper.
            search bestPredictor (currDepth + 1)

    // We initialize the search with a basic predictor, 
    // predicting average quality for every wine...
    let averageQuality = 
        sample 
        |> Seq.averageBy (fun (wine,value) -> value)
    let startPredictor (wine:Observation) = averageQuality

    // and we run the search:
    search startPredictor 0



(*
Step 4: Does it work?
+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

... only one way to know - let's try.
We'll pass every wine measurement available as a feature,
and see if the predictors get any better as we increase 
depth.
*)

let features : Feature list =
    [
        fun wine -> wine.Alcohol
        fun wine -> wine.Chlorides
        fun wine -> wine.``Citric acid``
        fun wine -> wine.Density
        fun wine -> wine.``Fixed acidity``
        fun wine -> wine.``Free sulfur dioxide``
        fun wine -> wine.PH
        fun wine -> wine.``Residual sugar``
        fun wine -> wine.Sulphates
        fun wine -> wine.``Total sulfur dioxide``
        fun wine -> wine.``Volatile acidity``
    ]

let sample = 
    redWines 
    |> Seq.map (fun wine -> wine,wine.Quality)

// [TODO] learn a model with (for example) a "search grid"
// with 10 levels, and a search depth of 5. 
let grid = 10
let depth = 5
let model = learn sample features grid depth
printfn "Cost: %.3f" (cost sample model)


// [TODO] Visualize actual vs predicted wine quality
// on a Scatter Chart.
redWines
|> Seq.map (fun wine -> wine.Quality, model wine) 
|> Chart.Scatter
|> Chart.WithOptions options
|> Chart.WithTitle "Quality vs. Predicted"
|> Chart.WithXTitle "Quality"
|> Chart.WithYTitle "Predicted"
|> Chart.Show



(*
Step 5: Cross Validation
+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

As we add depth, we add more stumps, correcting for small
prediction mistakes at each step. As a result, the cost
should decrease as we search deeper and deeper.
*)


// [TODO] How much better is our model as we increase 
// depth? 
[ 1 .. 10 ]
|> List.map (fun depth -> 
    depth, 
    learn sample features 5 depth |> cost sample)
|> Chart.Line
|> Chart.Show

// Let's split our sample in 2 halves. We will use the 
// first to train a model, and the second to test it.
let size = sample |> Seq.length
let train = sample |> Seq.take (size/2)
let test = sample |> Seq.skip (size/2)

// For depth 1 to 15, let's learn a model on train sample
// and compute the cost on the train and test sample.
[ 1 .. 15 ]
|> List.map (fun depth ->
    let model = learn train features 5 depth
    let trainCost = (depth,cost train model)
    let testCost = (depth,cost test model)
    trainCost,testCost)
|> List.unzip
|> fun (first,second) -> [first;second]
|> Chart.Line
|> Chart.Show

// You should see two things:
// 1) quality on test sample is not as good as on train
// 2) quality always improves with depth on train, but the
// results are less obvious on test.
// This is the main idea behind validation of machine
// learning models: your model learns a specific dataset,
// but how well it learns will not necessarily 
// "generalize", that is, a good fit on training data does
// not mean you will get good predictions on "new data".

// See readme.md for comments / conclusion, and pointers
// on how to go further.

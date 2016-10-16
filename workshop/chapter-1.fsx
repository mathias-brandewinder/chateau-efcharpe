(*
CHAPTER 1
+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

In this workshop, we will explore the wine dataset from the
University of California Irvine ML Repository:
https://archive.ics.uci.edu/ml/datasets/Wine+Quality

First, we will explore the data, to get a feel for it, and
see if we can spot relationships that could help us predict
wine quality.
*)


(*
TUTORIAL: F# Scripts
+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
If you are familiar with this, you can skip to next topic. 
*)

// first, install required packages by running paket:
// run install.cmd (windows) or install.sh

// the F# scripting environment allows you to select and
// directly execute code, seeing the results in the
// interactive window:

// select the code below and press alt+enter to execute: 
let x = 1 + 2

// the whole tutorial is written as 3 independent scripts.
// go through the scripts, running the code as you go.
// scripts contain "tutorial" sections, introducing F#
// syntax as you go.
// the [TODO] sections contain tasks you need to solve.

// [TODO] compute y = x + 1



(*
Step 1: using the CSV Type Provider to read data.
+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

We will try to predict the quality of a wine, based on its
chemical characteristics. We will use the Wine dataset from 
the University of California Irvine Machine Learning 
Repository.
The data is in CSV format. We will use the CSV type 
provider from fsharp.data to access the data.
*)

#r "../packages/FSharp.Data/lib/net40/FSharp.Data.dll"
open FSharp.Data

[<Literal>]
let redWinesPath = @"../data/winequality-red.csv"

type Wines = 
    CsvProvider<
        Sample = redWinesPath,
        Separators = ";",
        Schema = "float,float,float,float,float,float,float,float,float,float,float,float">
// for convenience, we create a type alias
type Wine = Wines.Row
// we can now read the data from the file, as a sequence:
let redWines = Wines.GetSample().Rows



(*
Tutorial: F# Sequences
+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
If you are familiar with this, you can skip to next topic. 

F# sequences represent a series of elements of one type.
You can manipulate them with the Seq module.
Sequences are lazy; they will compute only as needed. 
Sequence manipulations can be chained, using the |>
operator ("pipe-forward operator"), which feeds the 
previous result to the next operation. 
*)

let exSequence = [ 1; 2; 3; 4; 5; 4; 3; 2; 1 ]

let exMaximum = Seq.max exSequence

let exItem = Seq.item 3 exSequence

let exLazyMap = Seq.map (fun x -> x + 1) exSequence

let exMapResult = Seq.toArray exLazyMap

let exChaining = 
    exSequence
    // we keep only values > 2
    |> Seq.filter (fun x -> x > 2)
    // we keep only values < 5
    |> Seq.filter (fun x -> x < 5)
    // we double values with a map
    |> Seq.map (fun x -> x * 2)
    // we force evaluation
    |> Seq.toArray



(*
Step 2: using Seq to manipulate our data.
+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

We can now access rows from the dataset as a sequence, and
use Seq to analyze and manipulate the data.
*)

// let's inspect the 10th wine in the sample
let exampleWine = redWines |> Seq.item 10

// we have now typed access to wine properties
printfn "Wine quality: %.2f" exampleWine.Quality

// we can extract specific pieces of information
let averageQuality =
    redWines 
    // for each row/wine, extract the quality
    |> Seq.map (fun wine -> wine.Quality)
    // compute the sequence average
    |> Seq.average

// [TODO] what is the minimum quality? The maximum?
// [TODO] how many wines do we have in the sample?
// Hint: Seq.min, Seq.max, Seq.length might help

let minQuality = System.Double.NaN

let maxQuality = System.Double.NaN

let sampleSize = System.Int32.MaxValue



(*
Tutorial: XPlot charting library
+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

Displaying data as a chat helps understand it. We will use
the XPlot library, a wrapper over Google Charts, to 
visualize our wine dataset.
*)

#I "../packages/Newtonsoft.Json/lib/net45/"
#I "../packages/Google.DataTable.Net.Wrapper/lib/"
#r "../packages/XPlot.GoogleCharts/lib/net45/XPlot.GoogleCharts.dll"

open XPlot.GoogleCharts

[ (1.0,5.0); (2.0,7.0); (3.0,3.0); (4.0,10.0) ] 
|> Chart.Line
|> Chart.Show

[ (1.0,5.0); (2.0,7.0); (3.0,3.0); (4.0,10.0) ] 
|> Chart.Scatter
|> Chart.Show

[ ("Alpha",20.0); ("Beta",50.0); ("Charlie",10.0)]
|> Chart.Column
|> Chart.WithTitle "Some chart"
|> Chart.WithXTitle "Some columns"
|> Chart.WithYTitle "Some value"
|> Chart.Show

// Breaking it down: a Chart expects pairs of values,
// X and Y. We create a list of pairs, pass them 
// to the desired Chart, and Show the result.

// Pairs are represented by Tuples. A Tuple is just
// a pair of values, and can be used like this:

// creating a Tuple
let testTuple = ("FIRST",2)
// deconstructing a Tuple
let first,second = testTuple
// creating a... Triple? Truple?
let anotherTuple = ("Data",42,"More Data")
let a,b,c = anotherTuple



(*
Step 3: Visualizing our data with XPlot.
+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

We have data, we have charts - let's take a look into our
dataset.
*)

// plot wine density against sugar:
// we extract a tuple for each wine in the sequence
redWines
|> Seq.map (fun wine -> 
    wine.Density, wine.``Residual sugar``)
|> Chart.Scatter
|> Chart.Show

// we count how many wines fall in each quality group
redWines
|> Seq.countBy (fun wine -> wine.Quality)
|> Chart.Column
|> Chart.Show

// we want to predict quality using chemical measurements:
// could (for instance) density help predict quality?
redWines
|> Seq.map (fun wine -> wine.Density, wine.Quality)
|> Chart.Scatter
|> Chart.Show

// we have only 6 values for quality (3 to 8). As a result,
// points overlap, making it hard to understand how the
// data is laid out. Let's use transparency to fix that.
let options = Configuration.Options()
options.dataOpacity <- 0.10
options.pointSize <- 10

redWines
|> Seq.map (fun wine -> wine.Density, wine.Quality)
|> Chart.Scatter
|> Chart.WithOptions options
|> Chart.Show


// [TODO] create a Scatter chart, plotting different
// chemical characteristics against Quality.
// If you had to pick one to predict Quality, which
// one would you select?



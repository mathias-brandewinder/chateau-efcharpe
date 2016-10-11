# Chateau Efcharpe

This mini-workshop explores some machine learning ideas
via the Wine Quality dataset from the University of
California Irvine Machine Learning Repository:

https://archive.ics.uci.edu/ml/datasets/Wine+Quality

The goal is to predict the quality of a wine, based on 11
chemical measurements available. To do this, we will use
ideas from Ensemble Methods, an area of Machine Learning
focused on combining weak prediction models into good ones.

## How to use this

The workshop is organized in 3 independent scripts, Chapter
1, 2 and 3. The scripts can be run from the F# scripting
environment, without having to build anything.

Before diving into the first script, chapter-1.fsx, you 
will need to install 2 dependencies, fsharp.data and xplot.
To do this, you can run paket, or simply run install.cmd or
install.sh, depending on your operating system.

Once this is done, go to workshop/chapter-1.fsx, and start
following the steps, running the code as you go. Each
script contains tutorials, introducing some F# syntax, as 
well as sections marked [TODO], where you will have to 
write some code.

The folder solved/ contains a possible solution for each of
the scripts, in case you get stuck.

## Once you are done

The core ideas in this workshop are:

1) using Stumps as prediction models
2) using a Cost function to compare models
3) analyzing Residuals to improve a model, and combining
"weak" Stumps into a better model
4) splitting data between training and validation to 
evaluate the quality of a model

Put together, these ideas give us a fairly general
algorithm, which can be seen as a crude version of a more
general approach, called Gradient Boosting:
https://en.wikipedia.org/wiki/Gradient_boosting

Gradient Boosting takes these ideas further, by

1) generalizing this to arbitrary models, and not just 
stumps,
2) generalizing this to arbitrary cost functions, and not
just square-of-errors.

... and putting them in the context of a general method
called gradient descent.

If you are interested in seeing how this might look like,
you may find this blog series interesting:

http://brandewinder.com/2016/08/06/gradient-boosting-part-1/
http://brandewinder.com/2016/08/14/gradient-boosting-part-2/
http://brandewinder.com/2016/09/03/gradient-boosting-part-3/

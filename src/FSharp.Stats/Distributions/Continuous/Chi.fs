﻿namespace FSharp.Stats.Distributions.Continuous


open System
open FSharp.Stats
open FSharp.Stats.Distributions
open FSharp.Stats.Ops
open FSharp.Stats.SpecialFunctions

/// Chi distribution.
type Chi =
    
    // ChiSquared distribution helper functions.
    static member CheckParam dof = 
        if System.Double.IsNaN(dof)  || dof < 0. then 
            failwith "Chi distribution should be parametrized by degrees of Freedom in [0,inf)."
    
    /// Computes the mode.
    static member Mode dof =
        Chi.CheckParam dof
        sqrt(dof - 1.0)
        
        
    /// Computes the mean.
    static member Mean dof =
        Chi.CheckParam dof
        sqrt 2. * ((Gamma._gamma ((dof + 1.) / 2.))/(Gamma._gamma (dof / 2.)))
    
    /// Computes the variance.
    static member Variance dof =
        Chi.CheckParam dof
        let mean = sqrt 2. * ((Gamma._gamma ((dof + 1.) / 2.))/(Gamma._gamma (dof / 2.)))
        dof - pown mean 2
    
    /// Computes the standard deviation.
    static member StandardDeviation dof =
        Chi.CheckParam dof
        let mean = sqrt 2. * ((Gamma._gamma ((dof + 1.) / 2.))/(Gamma._gamma (dof / 2.)))
        let var = dof - pown mean 2
        sqrt var
    
    /// Produces a random sample using the current random number generator (from GetSampleGenerator()).
    static member Sample dof =
        Chi.CheckParam dof
        //rndgen.NextFloat() * (max - min) + min
        raise (NotImplementedException())

    /// Computes the probability density function.
    static member PDF dof x =
        Chi.CheckParam dof
        if x < 0.0 || dof < 1. then
            0.0
        else
            let gammaF = Gamma._gamma (dof/2.)
            let k = 2.**(dof/2. - 1.)
            let fraction = 1./((k)*gammaF)
            let ex1 = x**(dof-1.)
            let ex2 = exp(-(x**2.)/2.)
            let pdffunction = fraction*(ex1*ex2)
            pdffunction 
    
    /// Computes the cumulative distribution function.
    static member CDF dof x =
        Chi.CheckParam dof
        if dof = 0. then 
            if x > 0. then 1.
            else 0.
        else Gamma.lowerIncompleteRegularized (dof / 2.) ((x**2.) /2.)

    /// Computes the inverse cumulative distribution function (quantile function).
    static member InvCDF dof x =
        Chi.CheckParam dof
        failwithf "InvCDF not implemented yet"
    
    /// Returns the support of the exponential distribution: [0, Positive Infinity).
    static member Support dof =
        Chi.CheckParam dof
        (0., System.Double.PositiveInfinity)


    /// A string representation of the distribution.
    static member ToString dof =
        sprintf "Chi(dof = %f" dof

    /// Initializes a Chi distribution 
    static member Init dof =
        { new ContinuousDistribution<float,float> with
            member d.Mean              = Chi.Mean dof
            member d.StandardDeviation = Chi.StandardDeviation dof 
            member d.Variance          = Chi.Variance dof
            member d.CDF x             = Chi.CDF dof  x  
            member d.InvCDF x          = Chi.InvCDF dof  x  

            member d.Mode              = Chi.Mode dof
            member d.Sample ()         = Chi.Sample dof
            member d.PDF x             = Chi.PDF dof x           
            member d.Parameters        = DistributionParameters.Chi {DOF=dof}
            override d.ToString()      = Chi.ToString dof       
        }         



   


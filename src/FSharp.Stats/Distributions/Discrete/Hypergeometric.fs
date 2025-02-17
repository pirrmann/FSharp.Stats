﻿namespace FSharp.Stats.Distributions.Discrete

open FSharp.Stats
open FSharp.Stats.Distributions

// ######
// Hypergeometric distribution
// ----------------------------------------------
// wiki: "http://en.wikipedia.org/wiki/Hypergeometric_distribution"
// ######

// In probability theory and statistics, the hypergeometric distribution is a discrete probability distribution 
// that describes the probability of `k` successes (random draws for which the object 
// drawn has a specified feature) in `n` draws, without replacement, from a finite 
// population of size `N` that contains exactly `K` objects with that feature, 
// wherein each draw is either a success or a failure. In contrast, the binomial distribution 
// describes the probability of `k` successes in `n` draws with replacement.

// N is the population size,
// K is the number of success states in the population,
// n is the number of draws,
// k is the number of observed successes


/// Hypergeometric distribution
type Hypergeometric =

    // N ∈ {0,1,2,...}
    // K ∈ {0,1,2,...,N}
    // n ∈ {0,1,2,...,N}
    // Hypergeometric distribution helper functions.
    static member CheckParam N K n = 
        if N <= 0 || K <= 0 || n <= 0 || K > N || n > N then 
            failwith "Hypergeometric distribution should be parametrized by N, K and n > 0.0. Further K and n must be <= N"
    
    // k ∈ {max(0,n+K-N),...,min(n,K)}
    static member CheckParam_k N K n k =
        if k < 0 then failwith "k must be non negative integer number."
        if k > N then failwith "k cannot exceed N."
        if k > K then failwith "k cannot exceed K."
        if k > n then failwith "k cannot exceed n."

    /// Computes the mode.
    static member Mode N K n =
        Hypergeometric.CheckParam N K n
        (n + 1)*(K + 1)/(N + 2);

    /// Computes the mean.
    static member Mean N K n =
        Hypergeometric.CheckParam N K n

        float (K * n) / float N

    /// Computes the variance.
    static member Variance N K n =
        Hypergeometric.CheckParam N K n
        float (n * K * (N - n) * (N - K)) / float ((N * N * (N - 1)))

    /// Computes the standard deviation.
    static member StandardDeviation N K n =
        Hypergeometric.CheckParam N K n
        sqrt (Hypergeometric.Variance N K n)
            

    /// Produces a random sample using the current random number generator (from GetSampleGenerator()).
    /// No parameter checking!
    static member internal SampleUnchecked N K n =            
        let rec loop N K n x =
            if 0 = n then
                x
            else    
                let p = float K / float N
                let r = Random.rndgen.NextFloat()
                if r < p then 
                    loop (N-1) (K-1) (n-1) (x+1)
                else
                    loop (N-1) (K) (n-1) (x)
            
        loop N K n 0
            

    /// Produces a random sample using the current random number generator (from GetSampleGenerator()) and returns the number of success states `k`.
    static member Sample N K n =
        Hypergeometric.CheckParam N K n
        Hypergeometric.SampleUnchecked N K n


    // Rename PMF? https://en.wikipedia.org/wiki/Probability_mass_function
    // > A probability mass function differs from a probability density function (PDF) in that the latter is associated with continuous 
    // > rather than discrete random variables. A PDF must be integrated over an interval to yield a probability.

    /// Computes the probability mass function at k for P(X = k).
    static member PMF N K n k =
        Hypergeometric.CheckParam N K n
        Hypergeometric.CheckParam_k N K n k
        //(SpecialFunctions.Binomial.coeffcient K k) * (SpecialFunctions.Binomial.coeffcient (N-K) (n-k)) / (SpecialFunctions.Binomial.coeffcient N n)
        if (N-K)<(n-k) then 0. 
        else
            exp ((SpecialFunctions.Binomial._coeffcientLn K k) + (SpecialFunctions.Binomial._coeffcientLn (N-K) (n-k)) - SpecialFunctions.Binomial._coeffcientLn N n)
        
    /// Computes the cumulative distribution function at x, i.e. P(X &lt;= x).
    static member CDF N K n x =
        Hypergeometric.CheckParam N K n
        //Hypergeometric.CheckParam_k N K n k
        let k =  floor(x) |> int
        if (k < (max 0 (n + K - N))) then 
            0.0
        elif (k >= (min K n)) then
            1.0
        elif N-K < n then
            1.0
        else
            let d = SpecialFunctions.Binomial.coeffcientLn N n
            let rec loop i acc =
                if i <= k then
                    let tmp = exp ((SpecialFunctions.Binomial._coeffcientLn K i) + (SpecialFunctions.Binomial._coeffcientLn (N-K) (n-i)) - d)
                    loop (i+1) (acc+tmp)
                else
                    acc
            loop 0 0.0

    ///// <summary>
    /////   Fits the underlying distribution to a given set of observations.
    ///// </summary>
    //static member Fit K n (observations:float[]) =
    //    let successes = observations |> Array.sumBy (fun o -> if o = 1. then 1. else 0. )
    //    // Estimate N (population size)
    //    let N = System.Math.Truncate(n * K / successes)
    //    let K = System.Math.Truncate(successes * (N + 1.0) / n)

    ///// <summary>
    /////   Estimates a new Poisson distribution from a given set of observations.
    ///// </summary>
    //static member Estimate(observations:float[],?weights:float[]) =
    //    match weights with
    //    | None   -> observations |> Array.average
    //    | Some w -> observations |> Array.weightedMean w
    //    |> Poisson.Init  

    
    /// Computes the inverse cumulative distribution function (quantile function).
    static member InvCDF dof1 dof2 p =
        //if (p <= 0.0 || p > 1.0) then
        //    invalidArg "P" "Input must be between zero and one"
        //else
        //    let u = dof2 / (dof2 + dof1 * x)
        //    Beta.lowerIncomplete (dof2 * 0.5) (dof1 * 0.5) u
        failwithf "InvCDF not implemented yet"

    /// Returns the support of the hypergeometric distribution: (0., Positive Infinity).
    static member Support N K n =
        Hypergeometric.CheckParam N K n
        Interval.CreateClosed<int> ((max 0 (n + K - N) ),(min K n))


    

    /// A string representation of the distribution.
    static member ToString N K n  =
        sprintf "Hypergeometric(N = %i, K = %i, n = %i)" N K n 
    
    /// <summary> Initializes a hypergeometric distribution.
    /// 
    /// The hypergeometric distribution is a discrete probability distribution
    /// that describes the probability of `k` successes (random draws for which the object
    /// drawn has a specified feature) in `n` draws, without replacement, from a finite
    /// population of size `N` that contains exactly `K` objects with that feature,
    /// wherein each draw is either a success (`1.0`) or a failure (`0.0`).</summary>
    /// <param name="N">The population size</param>
    /// <param name="K">The number of success states in the population</param>
    /// <param name="n">The number of draws</param>
    static member Init N K n =
        { new DiscreteDistribution<float,int> with
            member d.Mean               = Hypergeometric.Mean N K n
            member d.StandardDeviation  = Hypergeometric.StandardDeviation N K n
            member d.Variance           = Hypergeometric.Variance N K n
            member d.CDF k              = Hypergeometric.CDF N K n k  
            member d.InvCDF k           = Hypergeometric.InvCDF N K n k  
            
            member d.Mode               = Hypergeometric.Mode N K n
            member d.Sample ()          = Hypergeometric.Sample N K n
            member d.PMF k              = Hypergeometric.PMF N K n k
            /// Computes the cumulative distribution function at k for P(X &lt;= k).
            member d.Parameters        = DistributionParameters.Hypergeometric {N=N;K=K;n=n}
            override d.ToString()       = Hypergeometric.ToString N K n                   
        }


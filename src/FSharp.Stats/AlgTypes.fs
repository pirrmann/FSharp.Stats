// (c) Microsoft Corporation 2005-2009. 

//----------------------------------------------------------------------------
// An implementation of generic dense and sparse matrix types.
//
// Overview and suffix documentation
//    _GU  = generic unspecialized (Matrix<T>, Vector<T> etc.) 
//    _GUA = generic unspecialized op on (underlying) array
//    _DS  = Double specialized (Matrix<float> = matrix, Vector<float> = vector etc.)
//
//    DM   = dense matrix
//    SM   = sparse matrix
//    V    = vector (dense)
//    RV   = row vector (dense)


//namespace Microsoft.FSharp.Math // old namespace
namespace FSharp.Stats

    #nowarn "60" // implementations in augmentations
    #nowarn "69" // implementations in augmentations

    //open Microsoft.FSharp.Math
    open System
    open System.Globalization
    open System.Collections
    open System.Collections.Generic
    open System.Diagnostics
    open Formatting
    //type permutation = int -> int


//=========================================================================
// (c) Microsoft Corporation 2005-2009. 
//=========================================================================

    [<AutoOpen>]
    module Helpers = 
        let sparseNYI() = failwith "this operation is not supported on sparse matrices"
        let sparseNotMutable() = failwith "sparse matrices are not mutable"
        
        [<RequiresExplicitTypeArguments>]
        let opsdata<'T> = GlobalAssociations.TryGetNumericAssociation<'T>()
        
        [<Literal>]
        let DenseMaxDisplay = 50
        [<Literal>]
        let VectorMaxDisplay = 100
    
    
    /// The value stored for the dictionary of numeric operations. If none is present then this indicates
    /// no operations are known for this type.
    type OpsData<'T> = INumeric<'T> option

    type DenseMatrix<'T>(opsData : OpsData<'T>, values : 'T[,]) = 
        member m.OpsData =  opsData
        member m.Values =  values
        member m.NumRows = values.GetLength(0)
        member m.NumCols = values.GetLength(1)

        member m.ElementOps = 
            match opsData with 
            | None -> raise (new System.NotSupportedException("The element type carried by this matrix does not support numeric operations"))
            | Some a -> a

        member m.Item
           with get (i,j) = values.[i,j]
           and  set (i,j) x = values.[i,j] <- x

    type SparseMatrix<'T>(opsData : OpsData<'T>, sparseValues : 'T array, sparseRowOffsets : int array, ncols:int, columnValues: int array) = 
        member m.OpsData = opsData; 
        member m.NumCols = ncols
        member m.NumRows = sparseRowOffsets.Length - 1
        member m.SparseColumnValues = columnValues
        member m.SparseRowOffsets =  sparseRowOffsets (* nrows + 1 elements *)
        member m.SparseValues =  sparseValues

        member m.ElementOps = 
              match opsData with 
              | None -> raise (new System.NotSupportedException("The element type carried by this matrix does not support numeric operations"))
              | Some a -> a

        member m.MinIndexForRow i = m.SparseRowOffsets.[i]
        member m.MaxIndexForRow i = m.SparseRowOffsets.[i+1]
              

        member m.Item 
            with get (i,j) = 
                let imax = m.NumRows
                let jmax = m.NumCols
                if j < 0 || j >= jmax || i < 0 || i >= imax then raise (new System.ArgumentOutOfRangeException()) else
                let kmin = m.MinIndexForRow i
                let kmax = m.MaxIndexForRow i
                let rec loopRow k =
                    (* note: could do a binary chop here *)
                    if k >= kmax then m.ElementOps.Zero else
                    let j2 = columnValues.[k]
                    if j < j2 then m.ElementOps.Zero else
                    if j = j2 then sparseValues.[k] else 
                    loopRow (k+1)
                loopRow kmin

#if FX_NO_DEBUG_DISPLAYS
#else
    [<System.Diagnostics.DebuggerDisplay("{DebugDisplay}")>]
#endif
    [<StructuredFormatDisplay("{StructuredDisplayAsFormattedMatrix}")>]
    [<CustomEquality; CustomComparison>]
    //[<System.Diagnostics.DebuggerTypeProxy(typedefof<MatrixDebugView<_>>)>]
    type Matrix<'T> = 
        | DenseRepr of DenseMatrix<'T>
        | SparseRepr of SparseMatrix<'T>
        interface System.IComparable
        interface IStructuralComparable
        interface IStructuralEquatable
        interface IEnumerable<'T> 
        interface IEnumerable
        interface IFsiFormattable
        interface IMatrixFormattable

        member m.ElementOps = match m with DenseRepr mr -> mr.ElementOps | SparseRepr mr -> mr.ElementOps
        member m.NumRows    = match m with DenseRepr mr -> mr.NumRows    | SparseRepr mr ->  mr.NumRows
        member m.NumCols    = match m with DenseRepr mr -> mr.NumCols    | SparseRepr mr ->  mr.NumCols

        member m.Item 
            with get (i,j) = 
                match m with 
                | DenseRepr dm -> dm.[i,j]
                | SparseRepr sm -> sm.[i,j]
            and set (i,j) x = 
              match m with 
              | DenseRepr dm -> dm.[i,j] <- x
              | SparseRepr _ -> sparseNotMutable()


#if FX_NO_DEBUG_DISPLAYS
#else
        [<DebuggerBrowsable(DebuggerBrowsableState.Collapsed)>]
#endif
        member m.IsDense = match m with DenseRepr _ -> true | SparseRepr _ -> false

#if FX_NO_DEBUG_DISPLAYS
#else
        [<DebuggerBrowsable(DebuggerBrowsableState.Collapsed)>]
#endif
        member m.IsSparse = match m with DenseRepr _ -> false | SparseRepr _ -> true

#if FX_NO_DEBUG_DISPLAYS
#else
        [<DebuggerBrowsable(DebuggerBrowsableState.Collapsed)>]
#endif
        member m.InternalSparseColumnValues = match m with DenseRepr _ -> invalidOp "not a sparse matrix" | SparseRepr mr -> mr.SparseColumnValues

#if FX_NO_DEBUG_DISPLAYS
#else
        [<DebuggerBrowsable(DebuggerBrowsableState.Collapsed)>]
#endif
        member m.InternalSparseRowOffsets = match m with DenseRepr _ -> invalidOp "not a sparse matrix" | SparseRepr mr -> mr.SparseRowOffsets

#if FX_NO_DEBUG_DISPLAYS
#else
        [<DebuggerBrowsable(DebuggerBrowsableState.Collapsed)>]
#endif
        member m.InternalSparseValues = match m with DenseRepr _ -> invalidOp "not a sparse matrix" | SparseRepr mr -> mr.SparseValues

#if FX_NO_DEBUG_DISPLAYS
#else
        [<DebuggerBrowsable(DebuggerBrowsableState.Collapsed)>]
#endif
        member m.InternalDenseValues = match m with DenseRepr mr -> mr.Values | SparseRepr _ -> invalidOp "not a dense matrix"

#if FX_NO_DEBUG_DISPLAYS
#else
    [<System.Diagnostics.DebuggerDisplay("{DebugDisplay}")>]
#endif
#if FX_NO_DEBUG_PROXIES
#else
    [<System.Diagnostics.DebuggerTypeProxy(typedefof<RowVectorDebugView<_>>)>]
#endif
    [<StructuredFormatDisplay("rowvec {StructuredDisplayAsArray}")>]
    [<Sealed>]
    type RowVector<'T>(opsRV : INumeric<'T> option, arrRV : 'T array ) =
        interface System.IComparable
        interface IStructuralComparable
        interface IStructuralEquatable 


#if FX_NO_DEBUG_DISPLAYS
#else
        [<DebuggerBrowsable(DebuggerBrowsableState.Collapsed)>]
#endif
        member x.InternalValues = arrRV
        member x.Values = arrRV
        member x.OpsData = opsRV
        
        
        interface IEnumerable<'T> with 
            member x.GetEnumerator() = (arrRV :> seq<_>).GetEnumerator()
        interface IEnumerable  with 
            member x.GetEnumerator() = (arrRV :> IEnumerable).GetEnumerator()

        member x.Length = arrRV.Length
        member x.NumCols = arrRV.Length
        member x.ElementOps = 
            match opsRV with 
            | None -> raise (new System.NotSupportedException("The element type carried by this row vector does not support numeric operations"))
            | Some a -> a

        member v.Item
           with get i = arrRV.[i]
           and  set i x = arrRV.[i] <- x

    and 
        [<Sealed>]
        RowVectorDebugView<'T>(v: RowVector<'T>)  =  

#if FX_NO_DEBUG_DISPLAYS
#else
             [<System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)>]
#endif
             member x.Items = v |> Seq.truncate 1 |> Seq.toArray 

#if FX_NO_DEBUG_DISPLAYS
#else
    [<System.Diagnostics.DebuggerDisplay("{DebugDisplay}")>]
#endif
#if FX_NO_DEBUG_PROXIES
#else
    [<System.Diagnostics.DebuggerTypeProxy(typedefof<VectorDebugView<_>>)>]
#endif
    [<StructuredFormatDisplay("vector {StructuredDisplayAsArray}")>]
    [<Sealed>]
    type Vector<'T>(opsV : INumeric<'T> option, arrV : 'T array) =

#if FX_NO_DEBUG_DISPLAYS
#else
        [<DebuggerBrowsable(DebuggerBrowsableState.Collapsed)>]
#endif
        member x.InternalValues = arrV
        member x.Values = arrV
        member x.OpsData = opsV
        interface System.IComparable
        interface IStructuralComparable
        interface IStructuralEquatable 

        interface IEnumerable<'T> with 
            member x.GetEnumerator() = (arrV :> seq<_>).GetEnumerator()
        interface IEnumerable  with 
            member x.GetEnumerator() = (arrV :> IEnumerable).GetEnumerator()
        
        ///Length of vector
        member m.Length = arrV.Length
        member m.NumRows = arrV.Length
        member m.ElementOps = 
            match opsV with 
            | None -> raise (new System.NotSupportedException("The element type carried by this vector does not support numeric operations"))
            | Some a -> a
        member v.Item
           with get i = arrV.[i]
           and  set i x = arrV.[i] <- x

#if FX_NO_DEBUG_PROXIES
#else
    and 
        [<Sealed>]
        VectorDebugView<'T>(v: Vector<'T>)  =  

             [<System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)>]
             member x.Items = v |> Seq.truncate 1 |> Seq.toArray 
#endif


    /// Implementations of operations that will work for any type
    module GenericImpl = 

        type OpsData<'T> = INumeric<'T> option

        let opsOfOpsData (d : OpsData<'T>)  =
             match d with 
             | None -> raise (new System.NotSupportedException("The element type '"+(typeof<'T>).ToString()+"' carried by this vector or matrix does not support numeric operations (i.e. does not have a registered numeric association)"))
             | Some a -> a

        let getNormOps (ops:INumeric<'T>) = 
            match box ops with
              | :? INormFloat<'T> as ops -> ops
              | _ -> raise (new System.NotSupportedException("The element type '"+(typeof<'T>.ToString())+"' carried by this vector or matrix does not support the INormFloat<_> operation (i.e. does not have a registered numeric association that supports this type)"))

        let mkDenseMatrixGU ops arr = DenseMatrix(ops,arr)
        let mkSparseMatrixGU ops (arr: 'a[,]) =
            let length1 = arr |> Array2D.length1
            let length2 = arr |> Array2D.length2

            let mutable nnz = 0
            let  a  = FSharp.Collections.ResizeArray<'a>()
            let  ja = FSharp.Collections.ResizeArray<int>()
            let  ia = FSharp.Collections.ResizeArray<int>()
            ia.Add(0)
            for i = 0 to (length1 - 1) do
                for j = 0 to (length2 - 1) do
                    if ((Array2D.get arr i j) |> System.Convert.ToDouble) >= 0.000001 || ((Array2D.get arr i j)|> System.Convert.ToDouble) <= -0.000001 then
                        a.Add((Array2D.get arr i j))
                        ja.Add(j)
                        nnz <- nnz + 1
                ia.Add(nnz)
            SparseMatrix(ops, a.ToArray(), ia.ToArray(), length2, ja.ToArray())
        let mkRowVecGU ops arr = RowVector(ops, arr)
        let mkVecGU ops arr = Vector(ops,arr)

        let inline getArray2D  (arrDM : _[,]) i j   = arrDM.[i,j]
        let inline setArray2D  (arrDM  : _[,]) i j x = arrDM.[i,j] <- x

        let inline createArray m = Array.zeroCreate m

        let inline createArray2D m n = Array2D.zeroCreate m n

        let inline assignArray2D m n f arr =  
            for i = 0 to m - 1 do 
                for j = 0 to n - 1 do 
                    (arr  : _[,]).[i,j] <- f i j

        let inline assignConstArray2D m n x arr =  
            for i = 0 to m - 1 do 
                for j = 0 to n - 1 do 
                    (arr  : _[,]).[i,j] <- x

        let inline assignDenseMatrixGU f (a:DenseMatrix<_>) = 
            assignArray2D a.NumRows a.NumCols f a.Values
        
        let inline assignArray m f (arr : _[]) = 
            for i = 0 to m - 1 do 
                arr.[i] <- f i

        let inline assignConstArray m x (arr : _[]) = 
            for i = 0 to m - 1 do 
                arr.[i] <- x

        let inline assignVecGU f (a:Vector<_>) = 
            assignArray a.NumRows f a.Values
        
        let inline assignRowVecGU f (a:RowVector<_>) = 
            assignArray a.NumCols f a.Values
        
        let createConstDenseMatrixGU ops m n x = 
            let arr = createArray2D m n 
            assignConstArray2D m n x arr;
            DenseMatrix(ops,arr)
        
        let createConstRowVecGU ops m x = 
            let arr = createArray m 
            assignConstArray m x arr;
            mkRowVecGU ops arr
        
        let createConstVecGU ops m x = 
            let arr = createArray m 
            assignConstArray m x arr;
            mkVecGU ops arr


        let inline createDenseMatrixGU ops m n f = (* inline eliminates unknown f call *)
            let arr = createArray2D m n 
            assignArray2D m n f arr;
            DenseMatrix(ops,arr)
        
        let createRowVecGU ops m f = 
            let arr = createArray m 
            assignArray m f arr;
            mkRowVecGU ops arr
        
        let inline createVecGU ops m f = (* inline eliminates unknown f call *)
            let arr = createArray m 
            assignArray m f arr;
            mkVecGU ops arr

        /// Create a matrix from a sparse sequence 
        let initSparseMatrixGU maxi maxj ops s = 

            (* nb. could use sorted dictionary but that is in System.dll *)
            let tab = Array.create maxi null
            let count = ref 0
            for (i,j,v) in s do
                if i < 0 || i >= maxi || j <0 || j >= maxj then failwith "initial value out of range";
                count := !count + 1;
                let tab2 = 
                    match tab.[i] with 
                    | null -> 
                        let tab2 = new Dictionary<_,_>(3) 
                        tab.[i] <- tab2;
                        tab2
                    | tab2 -> tab2
                tab2.[j] <- v
            // optimize this line....
            let offsA =  
               let rowsAcc = Array.zeroCreate (maxi + 1)
               let mutable acc = 0 
               for i = 0 to maxi-1 do 
                  rowsAcc.[i] <- acc;
                  acc <- match tab.[i] with 
                          | null -> acc
                          | tab2 -> acc+tab2.Count
               rowsAcc.[maxi] <- acc;
               rowsAcc
               
            let colsA,valsA = 
               let colsAcc = new ResizeArray<_>(!count)
               let valsAcc = new ResizeArray<_>(!count)
               for i = 0 to maxi-1 do 
                  match tab.[i] with 
                  | null -> ()
                  | tab2 -> tab2 |> Seq.toArray |> Array.sortBy (fun kvp -> kvp.Key) |> Array.iter (fun kvp -> colsAcc.Add(kvp.Key); valsAcc.Add(kvp.Value));
               colsAcc.ToArray(), valsAcc.ToArray()

            SparseMatrix(opsData=ops, sparseValues=valsA, sparseRowOffsets=offsA, ncols=maxj, columnValues=colsA)
        
        let zeroizeDenseMatrixGUA arr  m n : DenseMatrix<'T> = 
            let opsData = opsdata<'T> 
            let ops = opsOfOpsData opsData 
            let zero = ops.Zero 
            assignArray2D m n (fun _ _ -> zero) arr;
            DenseMatrix(opsData,arr)

        let zeroizeArray opsData arr m  = 
            let ops = opsOfOpsData opsData 
            let zero = ops.Zero 
            assignArray m (fun _ -> zero) arr

        let zeroizeVecGUA arr m  : Vector<'T> = 
            let opsData = opsdata<'T> 
            zeroizeArray opsData arr m;
            mkVecGU opsData arr

        let zeroizeRowVecGUA arr m  : RowVector<'T> = 
            let opsData = opsdata<'T> 
            zeroizeArray opsData arr m;
            mkRowVecGU opsData arr

        let listDenseMatrixGU ops xss =
            let m = List.length xss
            match xss with 
            | [] -> invalidArg "xss" "unexpected empty list"
            | h :: t -> 
              let n = List.length h
              if not (List.forall (fun xs -> List.length xs=n) t) then invalidArg "xss" "the lists are not all of the same length";
              let values = Array2D.zeroCreate m n
              List.iteri (fun i rw -> List.iteri (fun j x -> values.[i,j] <- x) rw) xss;
              DenseMatrix(ops,values)

        let colListDenseMatrixGU ops xss =
            let m = List.length xss
            match xss with 
            | [] -> invalidArg "xss" "unexpected empty list"
            | h :: t -> 
                let n = List.length h
                if not (List.forall (fun xs -> List.length xs=n) t) then invalidArg "xss" "the lists are not all of the same length";
                let values = Array2D.zeroCreate n m
                List.iteri (fun i rw -> List.iteri (fun j x -> values.[j,i] <- x) rw) xss;
                DenseMatrix(ops,values)

        let listVecGU ops xs = mkVecGU ops (Array.ofList xs)         
        let listRowVecGU ops xs = mkRowVecGU ops (Array.ofList xs) 

        let seqDenseMatrixGU ops xss = // TM
            //listDenseMatrixGU ops (xss |> Seq.toList |> List.map Seq.toList)
            let m = Seq.length xss
            if m < 1 then invalidArg "xss" "unexpected empty seq"
            let n = xss |> Seq.head |> Seq.length
            if not (Seq.forall (fun xs -> Seq.length xs=n) xss) then invalidArg "xss" "the sequences are not all of the same length";
            let values = Array2D.zeroCreate m n
            Seq.iteri (fun i rw -> Seq.iteri (fun j x -> values.[i,j] <- x) rw) xss;
            DenseMatrix(ops,values)

        let colSeqDenseMatrixGU ops xss = // TM
            //listDenseMatrixGU ops (xss |> Seq.toList |> List.map Seq.toList)
            let m = Seq.length xss
            if m < 1 then invalidArg "xss" "unexpected empty seq"
            let n = xss |> Seq.head |> Seq.length
            if not (Seq.forall (fun xs -> Seq.length xs=n) xss) then invalidArg "xss" "the sequences are not all of the same length";
            let values = Array2D.zeroCreate n m
            Seq.iteri (fun i rw -> Seq.iteri (fun j x -> values.[j,i] <- x) rw) xss;
            DenseMatrix(ops,values)

        let seqVecGU  ops xss = mkVecGU ops (Array.ofSeq xss) 
        let seqRowVecGU ops xss = mkRowVecGU ops (Array.ofSeq xss)

        let arrayDenseMatrixGU ops xss = // TM
            let m = Array.length xss
            if m < 1 then invalidArg "xss" "unexpected empty array"
            let n = xss.[0] |> Array.length
            if not (Array.forall (fun xs -> Array.length xs=n) xss) then invalidArg "xss" "the arrays are not all of the same length";
            let values = Array2D.zeroCreate m n
            Array.iteri (fun i rw -> Array.iteri (fun j x -> values.[i,j] <- x) rw) xss;
            DenseMatrix(ops,values)

        let colArrayDenseMatrixGU ops xss = // TM
            let m = Array.length xss
            if m < 1 then invalidArg "xss" "unexpected empty array"
            let n = xss.[0] |> Array.length
            if not (Array.forall (fun xs -> Array.length xs=n) xss) then invalidArg "xss" "the arrays are not all of the same length";
            let values = Array2D.zeroCreate n m
            Array.iteri (fun i rw -> Array.iteri (fun j x -> values.[j,i] <- x) rw) xss;
            DenseMatrix(ops,values)

        let inline binaryOpDenseMatrixGU f (a:DenseMatrix<_>) (b:DenseMatrix<_>) = (* pointwise binary operator *)
            let nA = a.NumCols
            let mA = a.NumRows
            let nB = b.NumCols 
            let mB = b.NumRows
            if nA<>nB || mA<>mB then invalidArg "a" "the two matrices do not have compatible dimensions";
            let arrA = a.Values 
            let arrB = b.Values 
            createDenseMatrixGU a.OpsData mA nA (fun i j -> f (getArray2D arrA i j) (getArray2D arrB i j))


        let nonZeroEntriesSparseMatrixGU  (a:SparseMatrix<_>) = 
            // This is heavily used, and this version is much faster than
            // the sequence operators.
            let entries = new ResizeArray<_>(a.SparseColumnValues.Length)
            let imax = a.NumRows
            let ops = a.ElementOps 
            let zero = ops.Zero
            for i in 0 .. imax - 1 do
              let kmin = a.MinIndexForRow i
              let kmax = a.MaxIndexForRow i
              for k in kmin .. kmax - 1 do
                  let j = a.SparseColumnValues.[k]
                  let v = a.SparseValues.[k]
                  if not (ops.Equals(v,zero)) then
                    entries.Add((i,j,v))
            (entries :> seq<_>)

        let nonzeroEntriesDenseMatrixGU  (a:DenseMatrix<_>) = 
            let imax = a.NumRows
            let jmax = a.NumCols
            let ops = a.ElementOps 
            let zero = ops.Zero
            seq { for i in 0 .. imax - 1 do 
                    for j in 0 .. jmax - 1 do 
                        let v = a.[i,j] 
                        if not (ops.Equals(v, zero)) then
                             yield (i,j,v) }


        // pointwise operation on two sparse matrices. f must be zero-zero-preserving, i.e. (f 0 0 = 0) 
        let binaryOpSparseMatrixGU f (a:SparseMatrix<_>) (b:SparseMatrix<_>) = 
            let ops = a.ElementOps 
            let zero = ops.Zero
            let imax1 = a.NumRows  
            let imax2 = b.NumRows
            let jmax1 = a.NumCols
            let jmax2 = b.NumCols
            if imax1 <> imax2 || jmax1 <> jmax2 then invalidArg "b" "the two matrices do not have compatible dimensions";
            let imin = 0
            let imax = imax1
            let jmax = jmax1
            let rowsR = Array.zeroCreate (imax+1)
            let colsR = new ResizeArray<_>(max a.SparseColumnValues.Length b.SparseColumnValues.Length)
            let valsR = new ResizeArray<_>(max a.SparseValues.Length b.SparseValues.Length)
            let rec loopRows i  = 
                rowsR.[i] <- valsR.Count;            
                if i >= imax1 then () else
                let kmin1 = a.MinIndexForRow i
                let kmax1 = a.MaxIndexForRow i 
                let kmin2 = b.MinIndexForRow i
                let kmax2 = b.MaxIndexForRow i
                let rec loopRow k1 k2  =
                    if k1 >= kmax1 && k2 >= kmax2 then () else
                    let j1 = if k1 >= kmax1 then jmax else a.SparseColumnValues.[k1]
                    let j2 = if k2 >= kmax2 then jmax else b.SparseColumnValues.[k2]
                    let v1 = if j1 <= j2 then a.SparseValues.[k1] else zero
                    let v2 = if j2 <= j1 then b.SparseValues.[k2] else zero
                    let jR = min j1 j2
                    let vR = f v1 v2
                    (* if vR <> zero then  *)
                    colsR.Add(jR);
                    valsR.Add(vR);
                    loopRow (if j1 <= j2 then k1+1 else k1) (if j2 <= j1 then k2+1 else k2)
                loopRow kmin1 kmin2;
                loopRows (i+1) 
            loopRows imin;
            SparseMatrix(opsData= a.OpsData, 
                         sparseRowOffsets=rowsR, 
                         ncols= a.NumCols, 
                         columnValues=colsR.ToArray(), 
                         sparseValues=valsR.ToArray())

        let inline binaryOpRowVecGU f (a:RowVector<_>) (b:RowVector<_>) = (* pointwise binary operator *)
            let mA = a.NumCols
            let mB = b.NumCols
            if mA<>mB then invalidArg "b" "the two vectors do not have compatible dimensions"
            createRowVecGU a.OpsData mA (fun i -> f a.[i] b.[i])

        let inline binaryOpVecGU f (a:Vector<_>) (b:Vector<_>) = (* pointwise binary operator *)
            let mA = a.NumRows
            let mB = b.NumRows
            if mA<>mB then invalidArg "b" "the two vectors do not have compatible dimensions"
            createVecGU a.OpsData mA (fun i -> f a.[i] b.[i])

        let inline unaryOpDenseMatrixGU f (a:DenseMatrix<_>) =
            let nA = a.NumCols 
            let mA = a.NumRows 
            let arrA = a.Values 
            createDenseMatrixGU a.OpsData mA nA (fun i j -> f (getArray2D arrA i j))

        let inline unaryOpRowVecGU f (a:RowVector<_>) =
            let mA = a.NumCols
            let arrA = a.Values 
            createRowVecGU a.OpsData mA (fun j -> f arrA.[j])

        let inline unaryOpVectorGU f (a:Vector<_>) =
            let mA = a.NumRows 
            let arrA = a.Values 
            createVecGU a.OpsData mA (fun i -> f arrA.[i])

        let unaryOpSparseGU f (a:SparseMatrix<_>) = (* pointwise zero-zero-preserving binary operator (f 0 = 0) *)
            SparseMatrix(opsData=a.OpsData,
                         sparseRowOffsets=Array.copy a.SparseRowOffsets, 
                         columnValues=Array.copy a.SparseColumnValues, 
                         sparseValues=Array.map f a.SparseValues, 
                         ncols=a.NumCols)

        // Strictly speaking, sparse arrays are non mutable so no copy is ever needed. But implementing it *)
        // anyway in case we move to mutability *)
        let copySparseGU (a:SparseMatrix<_>) = 
            SparseMatrix(opsData=a.OpsData,
                         sparseRowOffsets=Array.copy a.SparseRowOffsets, 
                         columnValues=Array.copy a.SparseColumnValues,
                         sparseValues=Array.copy a.SparseValues, 
                         ncols=a.NumCols)

        let addDenseMatrixGU  (a:DenseMatrix<_>)  b = let ops = a.ElementOps in binaryOpDenseMatrixGU (fun x y -> ops.Add(x, y)) a b
        let addSparseMatrixGU (a:SparseMatrix<_>) b = let ops = a.ElementOps in binaryOpSparseMatrixGU (fun x y -> ops.Add(x, y)) a b
        let addRowVecGU       (a:RowVector<_>)    b = let ops = a.ElementOps in binaryOpRowVecGU (fun x y -> ops.Add(x, y)) a b
        let addVecGU          (a:Vector<_>)       b = let ops = a.ElementOps in binaryOpVecGU  (fun x y -> ops.Add(x, y)) a b 

        let subDenseMatrixGU  (a:DenseMatrix<_>)  b = let ops = a.ElementOps in binaryOpDenseMatrixGU (fun x y -> ops.Subtract(x, y)) a b
        let subSparseMatrixGU (a:SparseMatrix<_>) b = let ops = a.ElementOps in binaryOpSparseMatrixGU (fun x y -> ops.Subtract(x, y)) a b
        let subRowVecGU       (a:RowVector<_>)    b = let ops = a.ElementOps in binaryOpRowVecGU (fun x y -> ops.Subtract(x, y)) a b
        let subVecGU          (a:Vector<_>)       b = let ops = a.ElementOps in binaryOpVecGU  (fun x y -> ops.Subtract(x, y)) a b 

        ///Point-wise multiplication 
        let cptMulDenseMatrixGU  (a:DenseMatrix<_>)  b = let ops = a.ElementOps in binaryOpDenseMatrixGU  (fun x y -> ops.Multiply(x, y)) a b
        let cptMulSparseMatrixGU (a:SparseMatrix<_>) b = let ops = a.ElementOps in binaryOpSparseMatrixGU  (fun x y -> ops.Multiply(x, y)) a b
        let cptMulRowVecGU       (a:RowVector<_>)    b = let ops = a.ElementOps in binaryOpRowVecGU (fun x y -> ops.Multiply(x, y)) a b
        let cptMulVecGU          (a:Vector<_>)       b = let ops = a.ElementOps in binaryOpVecGU  (fun x y -> ops.Multiply(x, y)) a b

        let cptMaxDenseMatrixGU  (a:DenseMatrix<_>) b = binaryOpDenseMatrixGU  max a b
        let cptMinDenseMatrixGU  (a:DenseMatrix<_>) b = binaryOpDenseMatrixGU  min a b
        let cptMaxSparseMatrixGU (a:SparseMatrix<_>) b = binaryOpSparseMatrixGU  max a b
        let cptMinSparseMatrixGU (a:SparseMatrix<_>) b = binaryOpSparseMatrixGU  min a b

        let cptMaxVecGU (a:Vector<_>) b = binaryOpVecGU max a b
        let cptMinVecGU (a:Vector<_>) b = binaryOpVecGU min a b

        let add (ops : INumeric<'T>) x y = ops.Add(x,y) 
        let sub (ops : INumeric<'T>) x y = ops.Subtract(x,y) 
        let mul (ops : INumeric<'T>) x y = ops.Multiply(x,y) 

        let inline foldR f z (a,b) = 
            let mutable res = z in
            for i = a to b do
                res <- f res i
            res

        let inline sumfR f (a,b) =
            let mutable res = 0.0 
            for i = a to b do
                res <- res + f i
            res
          

        let inline sumRGU (ops : INumeric<_>) f r = 
            let zero = ops.Zero 
            r |> foldR (fun z k -> add ops z (f k)) zero

        let genericMulDenseMatrix (a:DenseMatrix<_>) (b:DenseMatrix<_>) =
            let nA = a.NumCols 
            let mA = a.NumRows
            let nB = b.NumCols 
            let mB = b.NumRows
            if nA<>mB then invalidArg "b" "the two matrices do not have compatible dimensions"
            let ops = a.ElementOps 
            let arrA = a.Values 
            let arrB = b.Values 
            createDenseMatrixGU a.OpsData mA nB
              (fun i j -> (0,nA - 1) |> sumRGU ops (fun k -> mul ops (getArray2D arrA i k) (getArray2D arrB k j)))

        let debug = false

        let NormalizeOrdering (M:SparseMatrix<_>) = 
            for i = 0 to M.NumRows-1 do
                let index = M.SparseRowOffsets.[i]
                let count = M.SparseRowOffsets.[i+1] - index
                if count > 1 then
                    System.Array.Sort(M.SparseColumnValues, M.SparseValues, index, count)

        // Performs an inplace map with function x => X, skipping zero values
        let NormalizeZeros (M:SparseMatrix<_>) =
            let mutable nonZero = 0
            for row = 0 to M.NumRows - 1 do
                let startIndex = M.SparseRowOffsets.[row]
                let endIndex = M.SparseRowOffsets.[row + 1]
                M.SparseRowOffsets.[row] <- nonZero
                for j = startIndex to endIndex - 1 do
                    let item = M.SparseValues.[j]
                    if not(M.ElementOps.Equals(item, M.ElementOps.Zero)) then
                        M.SparseValues.[nonZero] <- item
                        M.SparseColumnValues.[nonZero] <- M.SparseColumnValues.[j]
                        nonZero <- nonZero + 1
            Array.truncate nonZero M.SparseColumnValues |> ignore
            Array.truncate nonZero M.SparseValues |> ignore
            M.SparseRowOffsets.[M.NumRows] <- nonZero

        let Normalize (M:SparseMatrix<_>) = 
            NormalizeOrdering M
            NormalizeZeros M

        // Sparse matrix multiplication algorithm. inline to get specialization at the 'double' type
        let inline genericMulSparse zero add mul (a:SparseMatrix<_>) (b:SparseMatrix<_>) =
            let ax = a.SparseValues
            let ap = a.SparseRowOffsets
            let ai = a.SparseColumnValues

            let bx = b.SparseValues
            let bp = b.SparseRowOffsets
            let bi = b.SparseColumnValues

            let rows = a.NumRows
            let cols = b.NumCols

            let cp = Array.zeroCreate (rows+1)

            let marker = Array.create cols -1

            let mutable count = 0
            for i = 0 to rows - 1 do
                for j= ap.[i] to ap.[i + 1]-1 do
                    // Row number to be added
                    let a = ai.[j]
                    for k = bp.[a] to bp.[a + 1] - 1 do
                        let b = bi.[k]
                        if not (marker.[b] = i) then
                            marker.[b] <- i
                            count <- count + 1
                cp.[i + 1] <- count
    
            let ci = Array.zeroCreate count
            let cx = Array.create count zero

            // Reset marker array
            for ib= 0 to cols-1 do
                marker.[ib] <- -1
            // Reset count
            count <- 0

            for i = 0 to rows - 1 do
                let rowStart = cp.[i]
                for j = ap.[i] to ap.[i + 1] - 1 do
                    let a = ai.[j]
                    let aEntry = ax.[j]
                    for k = bp.[a] to bp.[a + 1] - 1 do
                        let b = bi.[k]
                        let bEntry = bx.[k]
                        if marker.[b] < rowStart then
                            marker.[b] <- count
                            ci.[marker.[b]] <- b
                            cx.[marker.[b]] <- mul aEntry bEntry;
                            count <- count + 1
                        else
                            let prod = mul aEntry bEntry
                            cx.[marker.[b]] <- add cx.[marker.[b]] prod

            let matrix = SparseMatrix(opsData = a.OpsData,
                                     sparseRowOffsets= cp,
                                     ncols = cols,
                                     columnValues=ci,
                                     sparseValues=cx)
            Normalize matrix
            matrix

        let mulSparseMatrixGU (a: SparseMatrix<_>) b =
            let ops = a.ElementOps 
            let zero = ops.Zero
            genericMulSparse zero (add ops) (mul ops) a b


        let mulRowVecVecGU (a:RowVector<_>) (b:Vector<_>) =
            let mA = a.NumCols 
            let nB = b.NumRows 
            if mA<>nB then invalidArg "b" "the two vectors do not have compatible dimensions"
            let ops = a.ElementOps 
            (0,mA - 1) |> sumRGU ops (fun k -> mul ops a.[k] b.[k])

        let rowvecDenseMatrixGU (x:RowVector<_>) = createDenseMatrixGU x.OpsData 1         x.NumCols (fun _ j -> x.[j]) 
        let vectorDenseMatrixGU (x:Vector<_>)    = createDenseMatrixGU x.OpsData  x.NumRows 1         (fun i _ -> x.[i]) 

        let mulVecRowVecGU a b = genericMulDenseMatrix (vectorDenseMatrixGU a) (rowvecDenseMatrixGU b)

        let mulRowVecDenseMatrixGU (a:RowVector<_>) (b:DenseMatrix<_>) =
            let    nA = a.NumCols 
            let nB = b.NumCols
            let mB = b.NumRows 
            if nA<>mB then invalidArg "b" "the two vectors do not have compatible dimensions"
            let ops = a.ElementOps 
            let arrA = a.Values 
            let arrB = b.Values 
            createRowVecGU a.OpsData nB 
              (fun j -> (0,nA - 1) |> sumRGU ops (fun k -> mul ops arrA.[k] (getArray2D arrB k j)))

        let mulDenseMatrixVecGU (a:DenseMatrix<_>) (b:Vector<_>) =
            let nA = a.NumCols 
            let mA = a.NumRows 
            let mB    = b.NumRows
            if nA<>mB then invalidArg "b" "the two inputs do not have compatible dimensions"
            let ops = b.ElementOps 
            let arrA = a.Values 
            let arrB = b.Values 
            createVecGU b.OpsData mA
              (fun i -> (0,nA - 1) |> sumRGU ops (fun k -> mul ops (getArray2D arrA i k) arrB.[k]))

        let mulSparseVecGU (a:SparseMatrix<_>) (b:Vector<_>) =
            let nA = a.NumCols 
            let mA = a.NumRows 
            let mB    = b.NumRows 
            if nA<>mB then invalidArg "b" "the two inputs do not have compatible dimensions"
            let ops = b.ElementOps 
            let zero = ops.Zero
            createVecGU b.OpsData mA (fun i -> 
                let mutable acc = zero
                for k = a.MinIndexForRow i to a.MaxIndexForRow i - 1 do
                    let j = a.SparseColumnValues.[k]
                    let v = a.SparseValues.[k] 
                    acc <- add ops acc (mul ops v b.[j]);
                acc)

        let mulRVSparseMatrixGU (a:RowVector<_>) (b:SparseMatrix<_>) =
            let nA = b.NumCols
            let mA = b.NumRows 
            let mB    = a.NumCols 
            if mA<>mB then invalidArg "b" "the two inputs do not have compatible dimensions"
            let ops = b.ElementOps 
            let arr = createArray nA 
            zeroizeArray a.OpsData arr nA;
            for i = 0 to mA - 1 do
                for k = b.MinIndexForRow i to b.MaxIndexForRow i - 1 do
                    let j = b.SparseColumnValues.[k]
                    let v = b.SparseValues.[k] 
                    arr.[j] <- add ops arr.[j] (mul ops a.[i] v)
            mkRowVecGU a.OpsData arr


        let scaleDenseMatrixGU  k (a:DenseMatrix<_>)  = let ops = a.ElementOps in unaryOpDenseMatrixGU (fun x -> ops.Multiply(k,x)) a
        let scaleRowVecGU       k (a:RowVector<_>)    = let ops = a.ElementOps in unaryOpRowVecGU (fun x -> ops.Multiply(k,x)) a
        let scaleVecGU          k (a:Vector<_>)       = let ops = a.ElementOps in unaryOpVectorGU  (fun x -> ops.Multiply(k,x)) a
        let scaleSparseMatrixGU k (a:SparseMatrix<_>) = let ops = a.ElementOps in unaryOpSparseGU (fun x -> ops.Multiply(k,x)) a
        // add +        
        let addScalarDenseMatrixGU  k (a:DenseMatrix<_>)  = let ops = a.ElementOps in unaryOpDenseMatrixGU (fun x -> ops.Add(k,x)) a
        let addScalarRowVecGU       k (a:RowVector<_>)    = let ops = a.ElementOps in unaryOpRowVecGU (fun x -> ops.Add(k,x)) a
        let addScalarVecGU          k (a:Vector<_>)       = let ops = a.ElementOps in unaryOpVectorGU  (fun x -> ops.Add(k,x)) a
        let addScalarSparseMatrixGU k (a:SparseMatrix<_>) = let ops = a.ElementOps in unaryOpSparseGU (fun x -> ops.Add(k,x)) a
        // sub -        
        let subScalarDenseMatrixGU  k (a:DenseMatrix<_>)  = let ops = a.ElementOps in unaryOpDenseMatrixGU (fun x -> ops.Subtract(k,x)) a
        let subScalarRowVecGU       k (a:RowVector<_>)    = let ops = a.ElementOps in unaryOpRowVecGU (fun x -> ops.Subtract(k,x)) a
        let subScalarVecGU          k (a:Vector<_>)       = let ops = a.ElementOps in unaryOpVectorGU  (fun x -> ops.Subtract(k,x)) a
        let subScalarSparseMatrixGU k (a:SparseMatrix<_>) = let ops = a.ElementOps in unaryOpSparseGU (fun x -> ops.Subtract(k,x)) a

        let negDenseMatrixGU  (a:DenseMatrix<_>)  = let ops = a.ElementOps in unaryOpDenseMatrixGU (fun x -> ops.Negate(x)) a
        let negRowVecGU       (a:RowVector<_>)    = let ops = a.ElementOps in unaryOpRowVecGU (fun x -> ops.Negate(x)) a
        let negVecGU          (a:Vector<_>)       = let ops = a.ElementOps in unaryOpVectorGU  (fun x -> ops.Negate(x)) a
        let negSparseMatrixGU (a:SparseMatrix<_>) = let ops = a.ElementOps in unaryOpSparseGU (fun x -> ops.Negate(x)) a

        let mapDenseMatrixGU f (a : DenseMatrix<'T>) : DenseMatrix<'T> = 
            let arrA = a.Values 
            createDenseMatrixGU a.OpsData a.NumRows a.NumCols (fun i j -> f (getArray2D arrA i j))

        let mapVecGU f (a:Vector<_>) = 
            let mA= a.NumRows
            createVecGU a.OpsData mA (fun i -> f a.[i])

        let map2VecGU f (a:Vector<'a>) (b:Vector<'a>) : Vector<'a> = 
            let mA= if a.NumRows = b.NumRows then a.NumRows else raise (ArgumentException("Vectors of different length."))                        
            createVecGU a.OpsData mA (fun i -> f a.[i] b.[i])

        let map3VecGU f (a:Vector<'a>) (b:Vector<'a>) (c:Vector<'a>) : Vector<'a> = 
            let mA= if a.NumRows = b.NumRows && a.NumRows = c.NumRows then a.NumRows else raise (ArgumentException("Vectors of different length."))                        
            createVecGU a.OpsData mA (fun i -> f a.[i] b.[i] c.[i])
            
        let zipVecGU (a:Vector<'a>) (b:Vector<'b>) : Vector<'a*'b> = 
            let mA= if a.NumRows = b.NumRows then a.NumRows else raise (ArgumentException("Vectors of different length."))     
            createVecGU None mA (fun i -> a.[i],b.[i])
        
        let unzipVecGU (a : Vector<'a*'b>) : Vector<'a> * Vector<'b> = 
            let mA = a.NumRows
            createVecGU None mA (fun i -> fst a.[i]),createVecGU None mA (fun i -> snd a.[i])

        let copyDenseMatrixGU (a : DenseMatrix<'T>) : DenseMatrix<'T> = 
            let arrA = a.Values 
            createDenseMatrixGU a.OpsData a.NumRows a.NumCols (fun i j -> getArray2D arrA i j)

        let copyVecGU (a:Vector<_>) = 
            createVecGU a.OpsData a.NumRows (fun i -> a.[i])

        let copyRowVecGU (a:RowVector<_>) = 
            createRowVecGU a.OpsData a.NumCols (fun i -> a.[i])

        let toDenseSparseMatrixGU (a:SparseMatrix<_>) = 
            createDenseMatrixGU a.OpsData a.NumRows a.NumCols  (fun i j -> a.[i,j])
          
        let mapiDenseMatrixGU f (a: DenseMatrix<'T>) : DenseMatrix<'T> = 
            let arrA = a.Values 
            createDenseMatrixGU a.OpsData a.NumRows a.NumCols (fun i j -> f i j (getArray2D arrA i j))

        let mapRowVecGU f (a:RowVector<_>) = 
            createRowVecGU a.OpsData a.NumCols (fun i -> f a.[i])

        let mapiRowVecGU f (a:RowVector<_>) = 
            createRowVecGU a.OpsData a.NumCols (fun i -> f i a.[i])

        let mapiVecGU f (a:Vector<_>) = 
            createVecGU a.OpsData a.NumRows (fun i -> f i a.[i])

        let permuteVecGU (p:permutation) (a:Vector<_>) = 
            createVecGU a.OpsData a.NumRows (fun i -> a.[p i])

        let permuteRowVecGU (p:permutation) (a:RowVector<_>) = 
            createRowVecGU a.OpsData a.NumCols (fun i -> a.[p i])

        let inline inplace_mapDenseMatrixGU f (a:DenseMatrix<_>) = 
            let arrA = a.Values 
            assignDenseMatrixGU (fun i j -> f (getArray2D arrA i j)) a

        let inline inplace_mapRowVecGU f (a:RowVector<_>) = 
            assignRowVecGU (fun i -> f a.[i]) a

        let inline inplace_mapVecGU f (a:Vector<_>) = 
            assignVecGU (fun i -> f a.[i]) a

        let inline inplace_mapiDenseMatrixGU f (a:DenseMatrix<_>) = 
            let arrA = a.Values 
            assignDenseMatrixGU (fun i j -> f i j (getArray2D arrA i j)) a

        let inline inplace_mapiRowVecGU f (a:RowVector<_>) = 
            assignRowVecGU (fun i -> f i a.[i]) a

        let inline inplace_mapiVecGU f (a:Vector<_>) = 
            assignVecGU (fun i -> f i a.[i]) a

        let inline foldDenseMatrixGU f z (a:DenseMatrix<_>) =
            let nA = a.NumCols 
            let mA = a.NumRows
            let arrA = a.Values 
            let mutable acc = z
            for i = 0 to mA-1 do
                for j = 0 to nA-1 do 
                   acc <- f acc (getArray2D arrA i j)
            acc
        
        let inline foldVecGU f z (a:Vector<_>) =
            let mutable acc = z
            for i = 0 to a.NumRows-1 do acc <- f acc a.[i]
            acc
        
        let inline foldiDenseMatrixGU f z (a:DenseMatrix<_>) =
            let nA = a.NumCols 
            let mA = a.NumRows
            let arrA = a.Values 
            let mutable acc = z
            for i = 0 to mA-1 do
                for j = 0 to nA-1 do 
                   acc <- f i j acc (getArray2D arrA i j)
            acc
        
        let inline foldiVecGU f z (a:Vector<_>) =
            let mA = a.NumRows
            let mutable acc = z
            for i = 0 to mA-1 do acc <- f i acc a.[i]
            acc
        
        let rec forallR f (n,m) = (n > m) || (f n && forallR f (n+1,m))
        let rec existsR f (n,m) = (n <= m) && (f n || existsR f (n+1,m))
        
        let foralliDenseMatrixGU pred (a:DenseMatrix<_>) =
            let nA = a.NumCols 
            let mA = a.NumRows
            let arrA = a.Values 
            (0,mA-1) |> forallR  (fun i ->
            (0,nA-1) |> forallR  (fun j ->
            pred i j (getArray2D arrA i j)))

        let foralliVecGU pred (a:Vector<_>) =
            let mA = a.NumRows
            (0,mA-1) |> forallR  (fun i ->
            pred i a.[i])

        let existsiDenseMatrixGU pred (a:DenseMatrix<_>) =
            let nA = a.NumCols 
            let mA = a.NumRows
            let arrA = a.Values 
            (0,mA-1) |> existsR (fun i ->
            (0,nA-1) |> existsR (fun j ->
            pred i j (getArray2D arrA i j)))

        let existsiVecGU pred (a:Vector<_>) =
            let mA = a.NumRows
            (0,mA-1) |> existsR (fun i ->
            pred i a.[i])

        let sumDenseMatrixGU  (a:DenseMatrix<_>) = 
            let ops = a.ElementOps 
            foldDenseMatrixGU (fun acc aij -> add ops acc aij) ops.Zero a

        let sumSparseMatrixGU  (a:SparseMatrix<_>) = 
            let ops = a.ElementOps 
            a |> nonZeroEntriesSparseMatrixGU |> Seq.fold (fun acc (_,_,aij) -> add ops acc aij) ops.Zero

        let sumVecGU (a:Vector<_>) = 
            let ops = a.ElementOps 
            foldVecGU (fun acc ai -> add ops acc ai) ops.Zero a

        let prodDenseMatrixGU (a:DenseMatrix<_>) = 
            let ops = a.ElementOps 
            foldDenseMatrixGU (fun acc aij -> mul ops acc aij) ops.One a

        let prodSparseMatrixGU  (a:SparseMatrix<_>) = a |> toDenseSparseMatrixGU |> prodDenseMatrixGU

        let inline fold2DenseMatrixGU f z (a:DenseMatrix<_>) (b:DenseMatrix<_>) =
            let nA = a.NumCols 
            let mA = a.NumRows
            let nB = b.NumCols 
            let mB = b.NumRows
            if nA <> nB || mA <> mB then invalidArg "b" "the two matrices do not have compatible dimensions"
            let arrA = a.Values 
            let arrB = b.Values 
            let mutable acc = z
            for i = 0 to mA-1 do
                for j = 0 to nA-1 do 
                   acc <- f acc (getArray2D arrA i j) (getArray2D arrB i j)
            acc

        let inline fold2VecGU f z (a:Vector<_>) (b:Vector<_>) =
            let mA = a.NumRows
            let mB = b.NumRows
            if  mA <> mB then invalidArg "b" "the two vectors do not have compatible dimensions"
            let mutable acc = z
            for i = 0 to mA-1 do acc <- f acc a.[i] b.[i]
            acc

        let dotDenseMatrixGU (a:DenseMatrix<_>) b =
            let ops = a.ElementOps 
            fold2DenseMatrixGU (fun z va vb -> add ops z (mul ops va vb)) ops.Zero a b

        let dotVecGU (a:Vector<_>) b =
            let ops =   a.ElementOps
            let zero = ops.Zero 
            fold2VecGU  (fun z va vb -> add ops z (mul ops va vb)) zero a b 

        let normDenseMatrixGU (a:DenseMatrix<_>) = 
            let normOps = getNormOps a.ElementOps
            foldDenseMatrixGU (fun z aij -> z + ((normOps.Norm aij)**2.0)) 0.0 a |> sqrt

        let normSparseMatrixGU (a:SparseMatrix<_>) = 
            let normOps = getNormOps a.ElementOps
            a |> nonZeroEntriesSparseMatrixGU |> Seq.fold (fun acc (_,_,aij) -> acc + ((normOps.Norm aij)**2.0)) 0.0 |> sqrt

        let inplaceAddDenseMatrixGU  (a:DenseMatrix<_>) (b:DenseMatrix<_>) = 
            let ops = a.ElementOps 
            let arrB = b.Values 
            inplace_mapiDenseMatrixGU  (fun i j x -> add ops x (getArray2D arrB i j)) a
        
        let inplaceAddVecGU  (a:Vector<_>) (b:Vector<_>) = 
            let ops = a.ElementOps 
            inplace_mapiVecGU  (fun i x   -> add ops x b.[i]) a

        let inplaceAddRowVecGU (a:RowVector<_>) (b:Vector<_>) = 
            let ops = a.ElementOps 
            inplace_mapiRowVecGU (fun i x   -> add ops x b.[i]) a

        let inplaceSubDenseMatrixGU  (a:DenseMatrix<_>) (b:DenseMatrix<_>) = 
            let ops = a.ElementOps 
            let arrB = b.Values 
            inplace_mapiDenseMatrixGU  (fun i j x -> sub ops x (getArray2D  arrB i j)) a

        let inplaceSubVecGU (a:Vector<_>) (b:Vector<_>) = 
            let ops = a.ElementOps
            inplace_mapiVecGU  (fun i x   -> sub ops x b.[i]) a

        let inplaceSubRowVecGU (a:RowVector<_>) (b:Vector<_>) = 
            let ops = a.ElementOps 
            inplace_mapiRowVecGU (fun i x   -> sub ops x b.[i]  ) a

        let inplaceCptMulDenseMatrixGU  (a:DenseMatrix<_>) (b:DenseMatrix<_>) = 
            let ops = a.ElementOps 
            let arrB = b.Values 
            inplace_mapiDenseMatrixGU  (fun i j x -> mul ops x (getArray2D  arrB i j)) a

        let inplaceCptMulVecGU (a:Vector<_>) (b:Vector<_>) = 
            let ops = a.ElementOps  
            inplace_mapiVecGU  (fun i x   -> mul ops x b.[i]) a

        let inplaceCptMulRowVecGU (a:RowVector<_>) (b:Vector<_>) = 
            let ops = a.ElementOps 
            inplace_mapiRowVecGU (fun i x   -> mul ops x b.[i]  ) a

        let inplaceScaleDenseMatrixGU  x (a:DenseMatrix<_>) = 
            let ops = a.ElementOps 
            inplace_mapiDenseMatrixGU  (fun _ _ y -> ops.Multiply(x,y)) a

        let inplaceScaleVecGU  x (a:Vector<_>) = 
            let ops = a.ElementOps  
            inplace_mapiVecGU  (fun _ y   -> ops.Multiply(x,y)) a

        let inplaceScaleRowVecGU x (a:RowVector<_>) = 
            let ops = a.ElementOps 
            inplace_mapiRowVecGU (fun _ y   -> ops.Multiply(x,y)) a


        let wrapList (pre,mid,post,trim) show l = 
            let post = if trim then "; ..." + post else post
            match l with 
            | []    -> [pre;post]
            | [x]   -> [pre;show x;post]
            | x::xs -> [pre;show x] @ (List.collect (fun x -> [mid;show x]) xs) @ [post]

        let showItem opsData  x = 
            try 
              let ops = opsOfOpsData opsData 
              ops.ToString(x,"g10",System.Globalization.CultureInfo.InvariantCulture) 
            with :? System.NotSupportedException -> (box x).ToString()
        
        let mapR f (n,m) = if m < n then [] else List.init (m-n+1) (fun i -> f (n+i))

        let primShowDenseMatrixGU (sepX,sepR) (a : DenseMatrix<'e>) =
            let nA = min a.NumCols DenseMaxDisplay
            let mA = min a.NumRows DenseMaxDisplay
            let ops = a.OpsData 
            let showLine i = wrapList ("[",";","]", a.NumCols > nA) (showItem ops) ((0,nA-1) |> mapR  (fun j -> a.[i,j])) |> Array.ofList |> System.String.Concat
            wrapList (string nA + " " + string mA + "matrix [",";"+sepX,"]"+sepR, a.NumRows > mA) showLine [0..mA-1] |> Array.ofList |> System.String.Concat

        let showDenseMatrixGU     m = primShowDenseMatrixGU ("\n","\n") m
        let debugShowDenseMatrixGU m = primShowDenseMatrixGU (""  ,""  ) m
        
        let showVecGU s (a : Vector<_>) =
            let mA = min a.NumRows VectorMaxDisplay
            let ops = a.OpsData 
            wrapList (s+" [",";","]",a.NumRows > mA) (showItem ops) ((0,mA-1) |> mapR  (fun i -> a.[i])) |> Array.ofList |> System.String.Concat 

        let showRowVecGU s (a : RowVector<_>) =
            let mA = min a.NumCols VectorMaxDisplay
            let ops = a.OpsData 
            wrapList (s+" [",";","]",a.NumCols > mA) (showItem ops) ((0,mA-1) |> mapR  (fun i -> a.[i])) |> Array.ofList |> System.String.Concat 


    /// Implementations of operations specific to floating point types
    module DoubleImpl = 

        module GU = GenericImpl
        open Instances
        
        // Element type OpsData
        //type elem = float
        let zero = 0.0
        let one  = 1.0
        let inline sub (x:float) (y:float) = x - y
        let inline add (x:float) (y:float) = x + y
        let inline mul (x:float) (y:float) = x * y
        let inline neg (x:float) = -x

        // Specialized: these know the relevant set of 
        // ops without doing a table lookup based on runtime type
        let FloatOps = Some (FloatNumerics :> INumeric<float>)
        let inline initDenseMatrixDS m n f = GU.createDenseMatrixGU FloatOps m n f
        let inline createRowVecDS m f      = GU.createRowVecGU      FloatOps m f
        let inline createVecDS m f         = GU.createVecGU         FloatOps m f
        let inline mkDenseMatrixDS  arr    = GU.mkDenseMatrixGU     FloatOps arr
        let inline mkRowVecDS arr          = GU.mkRowVecGU          FloatOps arr
        let inline mkVecDS  arr            = GU.mkVecGU             FloatOps arr
        let inline listDenseMatrixDS  ll   = GU.listDenseMatrixGU   FloatOps ll
        let inline colListDenseMatrixDS ll = GU.colListDenseMatrixGU FloatOps ll
        let inline listRowVecDS l          = GU.listRowVecGU        FloatOps l
        let inline listVecDS  l            = GU.listVecGU           FloatOps l
        let inline seqDenseMatrixDS  ll    = GU.seqDenseMatrixGU    FloatOps ll
        let inline colSeqDenseMatrixDS  ll = GU.colSeqDenseMatrixGU FloatOps ll
        let inline arrayDenseMatrixDS  ll    = GU.arrayDenseMatrixGU    FloatOps ll
        let inline colArrayDenseMatrixDS  ll = GU.colArrayDenseMatrixGU FloatOps ll
        let inline seqRowVecDS l           = GU.seqRowVecGU         FloatOps l
        let inline seqVecDS  l             = GU.seqVecGU            FloatOps l

        let constDenseMatrixDS  m n x      = GU.createDenseMatrixGU  FloatOps m n (fun _ _ -> x)
        let constRowVecDS m x              = GU.createRowVecGU FloatOps m   (fun _ -> x)
        let constVecDS  m x                = GU.createVecGU  FloatOps m   (fun _ -> x)
        let scalarDenseMatrixDS   x        = constDenseMatrixDS  1 1 x 
        let scalarRowVecDS  x              = constRowVecDS 1   x 
        let scalarVecDS   x                = constVecDS  1   x 

        // Beware - when compiled with non-generic code createArray2D creates an array of null values,
        // not zero values. Hence the optimized version can only be used when compiling with generics.
        let inline zeroDenseMatrixDS m n = 
          let arr = GU.createArray2D m n 
          GU.mkDenseMatrixGU FloatOps arr
        // Specialized: these inline down to the efficient loops we need
        let addDenseMatrixDS     a b = GU.binaryOpDenseMatrixGU  add a b
        let addSparseDS     a b = GU.binaryOpSparseMatrixGU  add a b
        let addRowVecDS    a b = GU.binaryOpRowVecGU add a b
        let addVecDS     a b = GU.binaryOpVecGU  add a b
        let subDenseMatrixDS     a b = GU.binaryOpDenseMatrixGU  sub a b 
        let subSparseDS     a b = GU.binaryOpSparseMatrixGU  sub a b 
        let mulSparseDS     a b = GU.genericMulSparse zero add mul a b
        let subRowVecDS    a b = GU.binaryOpRowVecGU sub a b 
        let subVecDS     a b = GU.binaryOpVecGU  sub a b 
        let cptMulDenseMatrixDS  a b = GU.binaryOpDenseMatrixGU  mul a b
        let cptMulSparseDS  a b = GU.binaryOpSparseMatrixGU  mul a b
        let cptMulRowVecDS a b = GU.binaryOpRowVecGU mul a b
        let cptMulVecDS  a b = GU.binaryOpVecGU  mul a b
        type smatrix = SparseMatrix<float>
        type dmatrix = DenseMatrix<float>
        type vector = Vector<float>
        type rowvec = RowVector<float>
        let cptMaxDenseMatrixDS  (a:dmatrix) (b:dmatrix) = GU.binaryOpDenseMatrixGU  max a b
        let cptMinDenseMatrixDS  (a:dmatrix) (b:dmatrix) = GU.binaryOpDenseMatrixGU  min a b
        let cptMaxSparseDS  (a:smatrix) (b:smatrix) = GU.binaryOpSparseMatrixGU  max a b
        let cptMinSparseDS  (a:smatrix) (b:smatrix) = GU.binaryOpSparseMatrixGU  min a b
        let cptMaxVecDS  (a:vector) (b:vector) = GU.binaryOpVecGU  max a b
        let cptMinVecDS  (a:vector) (b:vector) = GU.binaryOpVecGU  min a b

        // Don't make any mistake about these ones re. performance.
        let mulDenseMatrixDS (a:dmatrix) (b:dmatrix) =
            let nA = a.NumCols 
            let mA = a.NumRows
            let nB = b.NumCols 
            let mB = b.NumRows
            if nA<>mB then invalidArg "b" "the two matrices do not have compatible dimensions"
            let arr = GU.createArray2D mA nB 
            let arrA = a.Values 
            let arrB = b.Values 
            for i = 0 to mA - 1 do 
                for j = 0 to nB - 1 do 
                    let mutable r = 0.0 
                    for k = 0 to mB - 1 do 
                        r <- r + mul (GU.getArray2D arrA i k) (GU.getArray2D arrB k j)
                    GU.setArray2D arr i j r
            mkDenseMatrixDS arr

        let mulRowVecDenseMatrixDS (a:rowvec) (b:dmatrix) =
            let nA = a.NumCols 
            let nB = b.NumCols 
            let mB = b.NumRows
            if nA<>mB then invalidArg "b" "the two inputs do not have compatible dimensions"
            let arr = Array.zeroCreate nB 
            let arrA = a.Values 
            let arrB = b.Values 
            for j = 0 to nB - 1 do 
                let mutable r = 0.0 
                for k = 0 to mB - 1 do 
                    r <- r + mul arrA.[k] (GU.getArray2D arrB k j)
                arr.[j] <- r
            mkRowVecDS arr

        let mulDenseMatrixVecDS (a:dmatrix) (b:vector) =
            let nA = a.NumCols 
            let mA = a.NumRows
            let mB = b.NumRows 
            if nA<>mB then invalidArg "b" "the two inputs do not have compatible dimensions"
            let arr = Array.zeroCreate mA 
            let arrA = a.Values 
            let arrB = b.Values 
            for i = 0 to mA - 1 do 
                let mutable r = 0.0 
                for k = 0 to nA - 1 do 
                    r <- r + mul (GU.getArray2D arrA i k) arrB.[k]
                arr.[i] <- r
            mkVecDS arr

        let mulRowVecVecDS (a:rowvec) (b:vector) =
            let nA = a.NumCols 
            let mB = b.NumRows 
            if nA<>mB then invalidArg "b" "the two vectors do not have compatible dimensions"
            let arrA = a.Values 
            let arrB = b.Values 
            let mutable r = 0.0 
            for k = 0 to nA - 1 do 
                r <- r + mul arrA.[k] arrB.[k]
            r

        let rowvecDenseMatrixDS (x:rowvec) = initDenseMatrixDS 1          x.NumCols (fun _ j -> x.[j]) 
        let vectorDenseMatrixDS (x:vector) = initDenseMatrixDS x.NumRows  1         (fun i _ -> x.[i]) 
        let mulVecRowVecDS a b = mulDenseMatrixDS (vectorDenseMatrixDS a) (rowvecDenseMatrixDS b) 

        let scaleDenseMatrixDS   k m = GU.unaryOpDenseMatrixGU  (fun x -> mul k x) m
        let scaleSparseDS   k m = GU.unaryOpSparseGU  (fun x -> mul k x) m
        let scaleRowVecDS  k m = GU.unaryOpRowVecGU (fun x -> mul k x) m
        let scaleVecDS   k m = GU.unaryOpVectorGU  (fun x -> mul k x) m
        // add + 
        let addScalarDenseMatrixDS   k m = GU.unaryOpDenseMatrixGU  (fun x -> add k x) m
        let addScalarSparseDS   k m = GU.unaryOpSparseGU  (fun x -> add k x) m
        let addScalarRowVecDS  k m = GU.unaryOpRowVecGU (fun x -> add k x) m
        let addScalarVecDS   k m = GU.unaryOpVectorGU  (fun x -> add k x) m
        // sub - 
        let subScalarDenseMatrixDS   k m = GU.unaryOpDenseMatrixGU  (fun x -> sub k x) m
        let subScalarSparseDS   k m = GU.unaryOpSparseGU  (fun x -> sub k x) m
        let subScalarRowVecDS  k m = GU.unaryOpRowVecGU (fun x -> sub k x) m
        let subScalarVecDS   k m = GU.unaryOpVectorGU  (fun x -> sub k x) m

        let negDenseMatrixDS     m   = GU.unaryOpDenseMatrixGU  (fun x -> neg x) m
        let negSparseDS     m   = GU.unaryOpSparseGU  (fun x -> neg x) m
        let negRowVecDS    m   = GU.unaryOpRowVecGU (fun x -> neg x) m
        let negVecDS     m   = GU.unaryOpVectorGU  (fun x -> neg x) m

        let traceDenseMatrixDS (a:dmatrix) =
            let nA = a.NumCols 
            let mA = a.NumRows
            if nA<>mA then invalidArg "a" "expected a square matrix";
            let arrA = a.Values 
            (0,nA-1) |> GU.sumfR (fun i -> GU.getArray2D arrA i i) 

        let sumDenseMatrixDS  a = GU.foldDenseMatrixGU add zero a
        let sumVecDS   a = GU.foldVecGU  add zero a
        let prodDenseMatrixDS a = GU.foldDenseMatrixGU mul one  a
        let prodVecDS  a = GU.foldVecGU  mul one  a

        let dotDenseMatrixDS a b = GU.fold2DenseMatrixGU (fun z va vb -> add z (mul va vb)) zero a b
        let dotVecDS a b = GU.fold2VecGU (fun z va vb -> add z (mul va vb)) zero a b
        let sumfDenseMatrixDS  f m = GU.foldDenseMatrixGU (fun acc aij -> add acc (f aij)) zero m
        let normDenseMatrixDS m = sqrt (sumfDenseMatrixDS (fun x -> x*x) m)

        let inplaceAddDenseMatrixDS  a (b:DenseMatrix<_>) = let arrB = b.Values  in GU.inplace_mapiDenseMatrixGU  (fun i j x -> x + GU.getArray2D arrB i j) a
        let inplaceAddVecDS    a (b:Vector<_>) = let arrB = b.Values  in GU.inplace_mapiVecGU  (fun i x   -> x + arrB.[i]) a
        let inplace_addRowVecDS a (b:RowVector<_>) = let arrB = b.Values in GU.inplace_mapiRowVecGU (fun i x   -> x + arrB.[i]) a
        let inplaceSubDenseMatrixDS  a (b:DenseMatrix<_>) = let arrB = b.Values  in GU.inplace_mapiDenseMatrixGU  (fun i j x -> x - GU.getArray2D  arrB i j) a
        let inplaceSubVecDS  a (b:Vector<_>) = let arrB = b.Values  in GU.inplace_mapiVecGU  (fun i x   -> x - arrB.[i]) a
        let inplace_subRowVecDS a (b:RowVector<_>) = let arrB = b.Values in GU.inplace_mapiRowVecGU (fun i x   -> x - arrB.[i]) a
        let inplaceCptMulDenseMatrixDS  a (b:DenseMatrix<_>) = let arrB = b.Values  in GU.inplace_mapiDenseMatrixGU  (fun i j x -> x * GU.getArray2D arrB i j) a
        let inplaceCptMulVecDS  a (b:Vector<_>) = let arrB = b.Values  in GU.inplace_mapiVecGU  (fun i x   -> x * arrB.[i]) a
        let inplace_cptMulRowVecDS a (b:RowVector<_>) = let arrB = b.Values in GU.inplace_mapiRowVecGU (fun i x   -> x * arrB.[i]) a
        let inplaceScaleDenseMatrixDS  (a:float) b = GU.inplace_mapiDenseMatrixGU  (fun _ _ x -> a * x) b
        let inplaceScaleVecDS  (a:float) b = GU.inplace_mapiVecGU  (fun _ x   -> a * x) b
        let inplace_scaleRowVecDS (a:float) b = GU.inplace_mapiRowVecGU (fun _ x   -> a * x) b



    /// Generic operations that, when used on floating point types, use the specialized versions in DoubleImpl
    module SpecializedGenericImpl = 

//        open Microsoft.FSharp.Math.Instances
//        open Microsoft.FSharp.Math.GlobalAssociations
        open FSharp.Stats.Instances
        open FSharp.Stats.GlobalAssociations

        module GU = GenericImpl
        module DS = DoubleImpl

          
        type smatrix = SparseMatrix<float>
        type dmatrix = DenseMatrix<float>
        type vector = Vector<float>
        type rowvec = RowVector<float>
        let inline dense x = DenseRepr(x)
        let inline sparse x = SparseRepr(x)
        let inline createMx  ops rows columns f = GU.createDenseMatrixGU ops rows columns f |> dense
        let inline createVx  ops m f   = GU.createVecGU ops m f
        let inline createRVx ops m f   = GU.createRowVecGU ops m f

        let nonZeroEntriesM a   = 
            match a with 
            | DenseRepr a -> GU.nonzeroEntriesDenseMatrixGU a 
            | SparseRepr a -> GU.nonZeroEntriesSparseMatrixGU a 

        /// Merge two sorted sequences
        let mergeSorted cf (s1: seq<'T>) (s2: seq<'b>) =
            seq { use e1 = s1.GetEnumerator()
                  use e2 = s2.GetEnumerator()
                  let havee1 = ref (e1.MoveNext())
                  let havee2 = ref (e2.MoveNext())
                  while !havee1 || !havee2 do
                    if !havee1 && !havee2 then
                        let v1 = e1.Current
                        let v2 = e2.Current
                        let c = cf v1 v2 
                        if c < 0 then 
                            do havee1 := e1.MoveNext()
                            yield Some(v1),None
                        elif c = 0 then
                            do havee1 := e1.MoveNext()
                            do havee2 := e2.MoveNext()
                            yield Some(v1),Some(v2)
                        else 
                            do havee2 := e2.MoveNext()
                            yield (None,Some(v2))
                    elif !havee1 then 
                        let v1 = e1.Current
                        do havee1 := e1.MoveNext()
                        yield (Some(v1),None)
                    else 
                        let v2 = e2.Current
                        do havee2 := e2.MoveNext()
                        yield (None,Some(v2)) }

        /// Non-zero entries from two sequences
        let mergedNonZeroEntriesM  (a:Matrix<_>) (b:Matrix<_>) = 
            let ops = a.ElementOps 
            let zero = ops.Zero
            mergeSorted (fun (i1,j1,_) (i2,j2,_) -> let c = compare i1 i2 in if c <> 0 then c else compare j1 j2) (nonZeroEntriesM a) (nonZeroEntriesM b)
            |> Seq.map (function | Some(i,j,v1),Some(_,_,v2) -> (v1,v2)
                                 | Some(i,j,v1),None         -> (v1,zero)
                                 | None,        Some(i,j,v2) -> (zero,v2)
                                 | None,        None          -> failwith "unreachable")


        
        // Creation
        let listM    xss : Matrix<'T>    = GU.listDenseMatrixGU opsdata<'T> xss |> dense
        let listV    xss : Vector<'T>    = GU.listVecGU         opsdata<'T> xss
        let listRV   xss : RowVector<'T> = GU.listRowVecGU      opsdata<'T> xss

        let arrayM    xss : Matrix<'T>    = GU.mkDenseMatrixGU  opsdata<'T> (Array2D.copy xss) |> dense
        let arraySM   xss : Matrix<'T>    = GU.mkSparseMatrixGU opsdata<'T> (Array2D.copy xss) |> sparse
        let arrayV    xss : Vector<'T>    = GU.mkVecGU          opsdata<'T> (Array.copy xss)
        let arrayRV   xss : RowVector<'T> = GU.mkRowVecGU       opsdata<'T> (Array.copy xss)
        
        let rowVecM xss: Matrix<'T>     = GU.rowvecDenseMatrixGU xss |> dense
        let vecM    xss: Matrix<'T>     = GU.vectorDenseMatrixGU xss |> dense

        let seqM    xss : Matrix<'T>    = GU.seqDenseMatrixGU   opsdata<'T> xss |> dense
        let seqCM   xss : Matrix<'T>    = GU.colSeqDenseMatrixGU   opsdata<'T> xss |> dense
        let seqV    xss : Vector<'T>    = GU.seqVecGU           opsdata<'T> xss
        let seqRV   xss : RowVector<'T> = GU.seqRowVecGU        opsdata<'T> xss

        let initM  m n f : Matrix<'T>    = GU.createDenseMatrixGU opsdata<'T> m n f |> dense
        let initRV m   f : RowVector<'T> = GU.createRowVecGU      opsdata<'T> m   f
        let initV  m   f : Vector<'T>    = GU.createVecGU         opsdata<'T> m   f

        let constM  m n x : Matrix<'T>    = GU.createConstDenseMatrixGU opsdata<'T> m n x |> dense
        let constRV m   x : RowVector<'T> = GU.createConstRowVecGU      opsdata<'T> m   x
        let constV  m   x : Vector<'T>    = GU.createConstVecGU         opsdata<'T> m   x

        let inline inplaceAssignM  f a = 
            match a with 
            | SparseRepr _ -> sparseNotMutable()
            | DenseRepr a -> GU.assignDenseMatrixGU  f a
        let inline assignV  f a = GU.assignVecGU  f a

        let coerce2 x = unbox(box(x))
        let loosenDM (x: dmatrix) : DenseMatrix<_>  = coerce2 x
        let loosenSM (x: smatrix) : SparseMatrix<_> = coerce2 x
        let loosenV  (x: vector)  : Vector<_>       = coerce2 x
        let loosenRV (x: rowvec)  : RowVector<_>    = coerce2 x
        let loosenF  (x: float)   : 'T              = coerce2 x

        let tightenDM (x: DenseMatrix<_>)  : dmatrix = coerce2 x
        let tightenSM (x: SparseMatrix<_>) : smatrix = coerce2 x
        let tightenV  (x: Vector<_>)       : vector  = coerce2 x
        let tightenRV (x: RowVector<_>)    : rowvec  = coerce2 x
        let tightenF  (x: 'T)              : float   = coerce2 x

        let zeroM m n = 
            let arr = GU.createArray2D m n
            // This is quite performance critical
            // Avoid assigining zeros into the array
            match box arr with 
            | :? (float[,])   as arr -> GU.mkDenseMatrixGU DS.FloatOps arr |> loosenDM |> dense
            | _ -> 
            GU.zeroizeDenseMatrixGUA arr m n  |> dense

        let zeroV m  : Vector<'T> = 
            let arr = GU.createArray m 
            // Avoid assigining zeros into the array
            match box (arr: 'T[]) with 
            | :? (float[])   as arr -> GU.mkVecGU DS.FloatOps arr |> loosenV
            | _ -> 
            GU.zeroizeVecGUA arr m

        let zeroRV m  : RowVector<'T> = 
            let arr = GU.createArray m 
            // Avoid assigining zeros into the array
            match box (arr: 'T[]) with 
            | :? (float[])   as arr -> GU.mkRowVecGU DS.FloatOps arr |> loosenRV
            | _ -> 
            GU.zeroizeRowVecGUA arr m
            
        let initNumericM m n f   = 
            let arr = GU.createArray2D m n 
            let opsData = opsdata<'T> 
            let ops = GU.opsOfOpsData opsData 
            GU.assignArray2D m n (f ops) arr;
            GU.mkDenseMatrixGU opsData arr |> dense

        let identityM m   = 
            let arr = GU.createArray2D m m 
            // This is quite performance critical
            // Avoid assigining zeros into the array
            match box arr with 
            | :? (float[,])   as arr -> 
                for i = 0 to m - 1 do 
                   arr.[i,i] <- 1.0 
                GU.mkDenseMatrixGU DS.FloatOps arr |> loosenDM |> dense
            | _ -> 
            let opsData = opsdata<'T> 
            let ops = GU.opsOfOpsData opsData 
            let zero = ops.Zero 
            let one = ops.One 
            GU.assignArray2D m m (fun i j -> if i = j then one else zero) arr;
            GU.mkDenseMatrixGU opsData arr |> dense

        let createNumericV m f  : Vector<'T> = 
            let arr = GU.createArray m 
            let opsData = opsdata<'T> 
            let ops = GU.opsOfOpsData opsData 
            GU.assignArray m (f ops) arr;
            GU.mkVecGU opsData arr
            
        let scalarM   x = constM 1 1 x 
        let scalarRV  x = constRV 1 x 
        let scalarV   x = constV 1 x 

        let diagnM (v:Vector<_>) n = 
            let ops = v.ElementOps
            let zero = ops.Zero 
            let nV = v.NumRows + (if n < 0 then -n else n) 
            createMx v.OpsData nV nV (fun i j -> if i+n=j then v.[i] else zero)

        let diagM v = diagnM v 0

        let constDiagM  n x : Matrix<'T> = 
            let opsData = opsdata<'T> 
            let ops = GU.opsOfOpsData opsData 
            let zero = ops.Zero 
            createMx opsData n n (fun i j -> if i=j then x else zero) 

        // Note: we drop sparseness on pointwise multiplication of sparse and dense.
        let inline binaryOpM opDenseDS opDenseGU opSparseDS opSparseMatrixGU a b = 
            match a,b with 
            | DenseRepr a,DenseRepr b -> 
                match box a with 
                | (:? dmatrix as a) -> opDenseDS   a (tightenDM b) |> loosenDM |> dense
                | _                 -> opDenseGU a b                           |> dense
            | SparseRepr a,SparseRepr b ->
                match box a with 
                | (:? smatrix as a) -> opSparseDS a (tightenSM b) |> loosenSM |> sparse
                | _                 -> opSparseMatrixGU a b                         |> sparse
            | SparseRepr a, DenseRepr b     -> opDenseGU (GU.toDenseSparseMatrixGU a) b         |> dense
            | DenseRepr  a, SparseRepr b    -> opDenseGU a (GU.toDenseSparseMatrixGU b)         |> dense

        let inline unaryOpM opDenseDS opDenseGU opSparseDS opSparseMatrixGU  b = 
            match b with 
            | DenseRepr b -> 
                match box b with 
                | (:? dmatrix as b)  -> opDenseDS b |> loosenDM |> dense
                | _                  -> opDenseGU b             |> dense
            | SparseRepr b ->             
                match box b with 
                | (:? smatrix as b) -> opSparseDS b |> loosenSM |> sparse
                | _                 -> opSparseMatrixGU b             |> sparse

        let inline floatUnaryOpM opDenseDS opDenseGU opSparseDS opSparseMatrixGU  b = 
            match b with 
            | DenseRepr b -> 
                match box b with 
                | (:? dmatrix as b)  -> opDenseDS b |> loosenF
                | _                  -> opDenseGU b             
            | SparseRepr b ->             
                match box b with 
                | (:? smatrix as b) -> opSparseDS b |> loosenF 
                | _                 -> opSparseMatrixGU b             

        let addM a b = binaryOpM DS.addDenseMatrixDS GU.addDenseMatrixGU DS.addSparseDS GU.addSparseMatrixGU a b
        let subM a b = binaryOpM DS.subDenseMatrixDS GU.subDenseMatrixGU DS.subSparseDS GU.subSparseMatrixGU a b
        let mulM a b = binaryOpM DS.mulDenseMatrixDS GU.genericMulDenseMatrix DS.mulSparseDS GU.mulSparseMatrixGU a b
        let cptMulM a b = binaryOpM DS.cptMulDenseMatrixDS GU.cptMulDenseMatrixGU DS.cptMulSparseDS GU.cptMulSparseMatrixGU a b
        let cptMaxM a b = binaryOpM DS.cptMaxDenseMatrixDS GU.cptMaxDenseMatrixGU DS.cptMaxSparseDS GU.cptMaxSparseMatrixGU a b
        let cptMinM a b = binaryOpM DS.cptMinDenseMatrixDS GU.cptMinDenseMatrixGU DS.cptMinSparseDS GU.cptMinSparseMatrixGU a b

        let addRV a b = 
            match box a with 
            | (:? rowvec as a) -> DS.addRowVecDS a (tightenRV b) |> loosenRV
            | _                -> GU.addRowVecGU a b

        let addV a b = 
            match box a with 
            | (:? vector as a) -> DS.addVecDS a (tightenV b) |> loosenV
            | _                -> GU.addVecGU a b

        let subRV a b = 
            match box a with 
            | (:? rowvec as a) -> DS.subRowVecDS   a (tightenRV b) |> loosenRV
            | _                -> GU.subRowVecGU a b

        let subV a b = 
            match box a with 
            | (:? vector as a) -> DS.subVecDS   a (tightenV b) |> loosenV
            | _                -> GU.subVecGU a b

        let mulRVM a b = 
            match b with 
            | DenseRepr b -> 
                match box a with 
                | (:? rowvec as a) -> DS.mulRowVecDenseMatrixDS   a (tightenDM b) |> loosenRV
                | _                -> GU.mulRowVecDenseMatrixGU a b
            | SparseRepr b -> GU.mulRVSparseMatrixGU a b

        let mulMV a b = 
            match a with 
            | DenseRepr a -> 
                match box a with 
                | (:? dmatrix as a) -> DS.mulDenseMatrixVecDS   a (tightenV b) |> loosenV
                | _                 -> GU.mulDenseMatrixVecGU a b
            | SparseRepr a -> GU.mulSparseVecGU a b 

        let mulRVV a b = 
            match box a with 
            | (:? rowvec as a) -> DS.mulRowVecVecDS   a (tightenV b) |> loosenF
            | _                -> GU.mulRowVecVecGU a b

        let mulVRV a b = 
            match box a with 
            | (:? vector as a) -> DS.mulVecRowVecDS   a (tightenRV b) |> loosenDM |> dense
            | _                -> GU.mulVecRowVecGU a b |> dense

        let cptMulRV a b = 
            match box a with 
            | (:? rowvec as a) -> DS.cptMulRowVecDS   a (tightenRV b) |> loosenRV
            | _                -> GU.cptMulRowVecGU a b

        let cptMulV a b = 
            match box a with 
            | (:? vector as a) -> DS.cptMulVecDS   a (tightenV b) |> loosenV
            | _                -> GU.cptMulVecGU a b

        let cptMaxV a b = 
            match box a with 
            | (:? vector as a) -> DS.cptMaxVecDS   a (tightenV b) |> loosenV
            | _                -> GU.cptMaxVecGU a b

        let cptMinV a b = 
            match box a with 
            | (:? vector as a) -> DS.cptMinVecDS   a (tightenV b) |> loosenV
            | _                -> GU.cptMinVecGU a b

        let scaleM a b = unaryOpM (fun b -> DS.scaleDenseMatrixDS (tightenF a) b) (GU.scaleDenseMatrixGU a)
                                  (fun b -> DS.scaleSparseDS (tightenF a) b) (GU.scaleSparseMatrixGU a) b

        let scaleRV a b = 
            match box b with 
            | (:? rowvec as b)  -> DS.scaleRowVecDS (tightenF a) b |> loosenRV 
            | _                 -> GU.scaleRowVecGU a b

        let scaleV a b = 
            match box b with 
            | (:? vector as b)  -> DS.scaleVecDS (tightenF a) b |> loosenV
            | _                 -> GU.scaleVecGU a b

        let addScalarM a b = unaryOpM (fun b -> DS.addScalarDenseMatrixDS (tightenF a) b) (GU.addScalarDenseMatrixGU a)
                                      (fun b -> DS.addScalarSparseDS (tightenF a) b) (GU.addScalarSparseMatrixGU a) b

        let addScalarRV a b = 
            match box b with 
            | (:? rowvec as b)  -> DS.addScalarRowVecDS (tightenF a) b |> loosenRV 
            | _                 -> GU.addScalarRowVecGU a b

        let addScalarV a b = 
            match box b with 
            | (:? vector as b)  -> DS.addScalarVecDS (tightenF a) b |> loosenV
            | _                 -> GU.addScalarVecGU a b

        let subScalarM a b = unaryOpM (fun b -> DS.subScalarDenseMatrixDS (tightenF a) b) (GU.subScalarDenseMatrixGU a)
                                      (fun b -> DS.subScalarSparseDS (tightenF a) b) (GU.subScalarSparseMatrixGU a) b

        let subScalarRV a b = 
            match box b with 
            | (:? rowvec as b)  -> DS.subScalarRowVecDS (tightenF a) b |> loosenRV 
            | _                 -> GU.subScalarRowVecGU a b

        let subScalarV a b = 
            match box b with 
            | (:? vector as b)  -> DS.subScalarVecDS (tightenF a) b |> loosenV
            | _                 -> GU.subScalarVecGU a b
        
        let dotM a b = 
            match a,b with 
            | DenseRepr a,DenseRepr b -> 
                match box b with 
                | (:? dmatrix as b)  -> DS.dotDenseMatrixDS   (tightenDM a) b |> loosenF
                | _                  -> GU.dotDenseMatrixGU a b
            | _ ->  
                let ops = a.ElementOps 
                mergedNonZeroEntriesM a b |> Seq.fold (fun z (va,vb) -> GU.add ops z (GU.mul ops va vb)) ops.Zero 

        let dotV a b = 
            match box b with 
            | (:? vector as b)  -> DS.dotVecDS   (tightenV a) b |> loosenF
            | _                 -> GU.dotVecGU a b

        let negM a = unaryOpM DS.negDenseMatrixDS GU.negDenseMatrixGU DS.negSparseDS GU.negSparseMatrixGU a

        let negRV a = 
            match box a with 
            | (:? rowvec as a) -> DS.negRowVecDS a |> loosenRV
            | _               ->  GU.negRowVecGU a

        let negV a = 
            match box a with 
            | (:? vector as a) -> DS.negVecDS a |> loosenV
            | _               ->  GU.negVecGU a

        let traceMGU (a:Matrix<_>) =
            let nA = a.NumCols  
            let mA = a.NumRows 
            if nA<>mA then invalidArg "a" "expected a square matrix";
            let ops = a.ElementOps 
            (0,nA-1) |> GU.sumRGU ops (fun i -> a.[i,i]) 

        let traceM a = floatUnaryOpM DS.traceDenseMatrixDS (dense >> traceMGU) (sparse >> traceMGU) (sparse >> traceMGU) a
        let sumM a = floatUnaryOpM DS.sumDenseMatrixDS GU.sumDenseMatrixGU GU.sumSparseMatrixGU GU.sumSparseMatrixGU a
        let prodM a = floatUnaryOpM DS.prodDenseMatrixDS GU.prodDenseMatrixGU GU.prodSparseMatrixGU GU.prodSparseMatrixGU a
        let normM a = floatUnaryOpM DS.normDenseMatrixDS GU.normDenseMatrixGU GU.normSparseMatrixGU GU.normSparseMatrixGU a

        let opsM a = 
            match a with 
            | DenseRepr a -> a.OpsData 
            | SparseRepr a -> a.OpsData 
        
        let transM a = 
            match a with 
            | DenseRepr a -> 
                // rows of transposed matrix = columns of original matrix and vice versa
                createMx a.OpsData a.NumCols a.NumRows (fun i j -> a.[j,i])
            | SparseRepr a -> 
                a |> GU.nonZeroEntriesSparseMatrixGU  |> Seq.map (fun (i,j,v) -> (j,i,v)) |> GU.initSparseMatrixGU a.NumCols a.NumRows a.OpsData |> sparse
        
        let permuteRows (p: permutation) a =
            match a with
            | DenseRepr a ->
                createMx a.OpsData a.NumRows a.NumCols (fun i j -> a.[p i,j])
            | SparseRepr a ->
                a |> GU.nonZeroEntriesSparseMatrixGU  |> Seq.map (fun (i,j,v) -> (p i,j,v)) |> GU.initSparseMatrixGU a.NumCols a.NumRows a.OpsData |> sparse

        let permuteColumns (p: permutation) a =
            match a with
            | DenseRepr a ->
                createMx a.OpsData a.NumRows a.NumCols (fun i j -> a.[i,p j])
            | SparseRepr a ->
                a |> GU.nonZeroEntriesSparseMatrixGU  |> Seq.map (fun (i,j,v) -> (i,p j,v)) |> GU.initSparseMatrixGU a.NumCols a.NumRows a.OpsData |> sparse

        let transRV (a:RowVector<_>) = 
            createVx a.OpsData  a.NumCols (fun i -> a.[i])

        let transV (a:Vector<_>)  = 
            createRVx a.OpsData a.NumRows (fun i -> a.[i])

        let inplaceAddM a b = 
            match a,b with 
            | DenseRepr a,DenseRepr b -> 
                match box a with 
                | (:? dmatrix as a) -> DS.inplaceAddDenseMatrixDS   a (tightenDM b)
                | _                 -> GU.inplaceAddDenseMatrixGU a b
            | _ -> sparseNotMutable()

        let inplaceAddV a b = 
            match box a with 
            | (:? vector as a) -> DS.inplaceAddVecDS   a (tightenV b)
            | _                -> GU.inplaceAddVecGU a b

        let inplaceSubM a b = 
            match a,b with 
            | DenseRepr a,DenseRepr b -> 
                match box a with 
                | (:? dmatrix as a) -> DS.inplaceSubDenseMatrixDS   a (tightenDM b)
                | _                -> GU.inplaceSubDenseMatrixGU a b
            | _ -> sparseNotMutable()

        let inplaceSubV a b = 
            match box a with 
            | (:? vector as a) -> DS.inplaceSubVecDS   a (tightenV b)
            | _                -> GU.inplaceSubVecGU a b


        let inplaceCptMulM a b = 
            match a,b with 
            | DenseRepr a,DenseRepr b -> 
                match box a with 
                | (:? dmatrix as a) -> DS.inplaceCptMulDenseMatrixDS   a (tightenDM b)
                | _                -> GU.inplaceCptMulDenseMatrixGU a b
            | _ -> sparseNotMutable()

        let inplaceCptMulV a b = 
            match box a with 
            | (:? vector as a) -> DS.inplaceCptMulVecDS   a (tightenV b)
            | _                -> GU.inplaceCptMulVecGU a b

        let inplaceScaleM a b = 
            match b with 
            | DenseRepr b -> 
                match box b with 
                | (:? dmatrix as b)  -> DS.inplaceScaleDenseMatrixDS   (tightenF a) b
                | _                 -> GU.inplaceScaleDenseMatrixGU a b
            | _ -> sparseNotMutable()

        let inplaceScaleV a b = 
            match box b with 
            | (:? vector as b)  -> DS.inplaceScaleVecDS   (tightenF a) b
            | _                 -> GU.inplaceScaleVecGU a b

        let existsM  f a = 
            match a with 
            | SparseRepr _ -> sparseNYI() // note: martin says "run f on a token element if it's not full"
            | DenseRepr a -> GU.existsiDenseMatrixGU  (fun _ _ -> f) a

        let existsV  f a = GU.existsiVecGU  (fun _ -> f) a

        let forallM  f a = 
            match a with 
            | SparseRepr _ -> sparseNYI()
            | DenseRepr a -> GU.foralliDenseMatrixGU  (fun _ _ -> f) a

        let forallV  f a = GU.foralliVecGU  (fun _ -> f) a

        let existsiM  f a = 
            match a with 
            | SparseRepr _ -> sparseNYI()
            | DenseRepr a -> GU.existsiDenseMatrixGU  f a

        let existsiV  f a = GU.existsiVecGU  f a

        let foralliM  f a = 
            match a with 
            | SparseRepr _ -> sparseNYI()
            | DenseRepr a -> GU.foralliDenseMatrixGU  f a

        let foralliV  f a = GU.foralliVecGU  f a

        let mapM  f a = 
            match a with 
            | SparseRepr _ -> sparseNYI()
            | DenseRepr a -> DenseRepr(GU.mapDenseMatrixGU f a)

        let mapV  f a = GU.mapVecGU f a

        let map2V  f a b = GU.map2VecGU f a b

        let map3V f a b c = GU.map3VecGU f a b c

        let zipV a b = GU.zipVecGU a b

        let unzipV a = GU.unzipVecGU a

        let copyM  a = 
            match a with 
            | SparseRepr a -> SparseRepr (GU.copySparseGU a)
            | DenseRepr a -> DenseRepr (GU.copyDenseMatrixGU a)

        let copyV  a = GU.copyVecGU a

        let copyRV  a = GU.copyRowVecGU a

        let mapiM  f a = 
            match a with 
            | SparseRepr _ -> sparseNYI()
            | DenseRepr a -> DenseRepr (GU.mapiDenseMatrixGU f a)

        let mapiV  f a = GU.mapiVecGU f a
        let permuteV p a = GU.permuteVecGU p a
        let permuteRV p a = GU.permuteRowVecGU p a
        
        let mapRV f a = GU.mapRowVecGU f a

        let mapiRV  f a = GU.mapiRowVecGU f a

        let toDenseM a = 
            match a with 
            | SparseRepr a -> GU.toDenseSparseMatrixGU a |> dense
            | DenseRepr _ -> a
        let toSparseM (a: Matrix<'T>) =
            match a with 
            | SparseRepr _ -> a
            | DenseRepr a ->
                let length1 = a.NumRows
                let length2 = a.NumCols

                let mutable nnz = 0
                let  ar = FSharp.Collections.ResizeArray<'T>()
                let  ja = FSharp.Collections.ResizeArray<int>()
                let  ia = FSharp.Collections.ResizeArray<int>()
                ia.Add(0)

                for i = 0 to (length1 - 1) do
                    for j = 0 to (length2 - 1) do
                        if (a.Item(i, j)|> System.Convert.ToDouble) >= 0.000001 || (a.Item(i, j)|> System.Convert.ToDouble) <= -0.000001 then
                            ar.Add(a.Item(i, j))
                            ja.Add(j)
                            nnz <- nnz + 1
                    ia.Add(nnz)
                SparseRepr (SparseMatrix(opsdata<'T>, ar.ToArray(), ia.ToArray(), length2, ja.ToArray()))

        let initSparseM i j x : Matrix<'T> = 
            let opsData = opsdata<'T> 
            GU.initSparseMatrixGU i j opsData x |> sparse
          
        let initDenseM i j x : Matrix<'T> = 
            let r = zeroM i j
            x |> Seq.iter (fun (i,j,v) -> r.[i,j] <- v);
            r

        let getDiagnM (a:Matrix<_>) n =
            let nA = a.NumCols 
            let mA = a.NumRows
            if nA<>mA then invalidArg "a" "expected a square matrix";
            let ni = if n < 0 then -n else 0 
            let nj = if n > 0 then  n else 0 
            GU.createVecGU (opsM a) (max (nA-abs(n)) 0) (fun i -> a.[i+ni,i+nj]) 

        let getDiagM  a = getDiagnM a 0

        let inline inplace_mapM  f a = 
            match a with 
            | SparseRepr _ -> sparseNotMutable()
            | DenseRepr a -> GU.inplace_mapDenseMatrixGU f a

        let inline inplace_mapiM  f a = 
            match a with 
            | SparseRepr _ -> sparseNotMutable()
            | DenseRepr a -> GU.inplace_mapiDenseMatrixGU f a

        let inline inplace_mapV  f a = GU.inplace_mapVecGU f a
 
        let inline inplace_mapiV  f a = GU.inplace_mapiVecGU f a
        
        let inline foldM  f z a = 
            match a with 
            | SparseRepr _ -> sparseNYI()
            | DenseRepr a -> GU.foldDenseMatrixGU f z a

        let inline foldV  f z a = GU.foldVecGU f z a

        let inline foldiM  f z a = 
            match a with 
            | SparseRepr _ -> sparseNYI()
            | DenseRepr a -> GU.foldiDenseMatrixGU f z a

        let inline foldiV  f z a = GU.foldiVecGU f z a

        let compareM (comp: IComparer) (a:Matrix<'T>) (b:Matrix<'T>) = 
            let nA = a.NumCols 
            let mA = a.NumRows 
            let nB = b.NumCols 
            let mB = b.NumRows 
            let c = compare mA mB 
            if c <> 0 then c else
            let c = compare nA nB 
            if c <> 0 then c else
            match a,b with 
            | DenseRepr a, DenseRepr b -> 
              let rec go2 i j = 
                 if j < nA then 
                   let c = comp.Compare( a.[i,j], b.[i,j])
                   if c <> 0 then c else 
                   go2 i (j+1) 
                 else 0 
              let rec go1 i = 
                 if i < mA then 
                   let c = go2 i 0 
                   if c <> 0 then c 
                   else go1 (i+1) 
                 else 0 
              go1 0
            | _ -> 
              match (mergedNonZeroEntriesM a b |> Seq.tryPick (fun (v1,v2) -> let c = comp.Compare(v1,v2) in if c = 0 then None else Some(c))) with
              | None -> 0
              | Some(c) -> c
             
        let equalsM (comp: IEqualityComparer) (a:Matrix<'T>) (b:Matrix<'T>) = 
            let nA = a.NumCols 
            let mA = a.NumRows 
            let nB = b.NumCols 
            let mB = b.NumRows 
            (mA = mB ) && (nA = nB) && 
            match a,b with 
            | DenseRepr a, DenseRepr b -> 
                let rec go2 i j =  j >= nA || (comp.Equals( a.[i,j], b.[i,j]) && go2 i (j+1) )
                let rec go1 i = i >= mA || (go2 i 0  && go1 (i+1))
                go1 0
            | _ -> 
                mergedNonZeroEntriesM a b |> Seq.forall (fun (v1,v2) -> comp.Equals(v1,v2))
             

        let compareV (comp: IComparer) (a:Vector<'T>) (b:Vector<'T>) = 
            let mA = a.NumRows
            let mB = b.NumRows 
            let c = compare mA mB 
            if c <> 0 then c else
            let rec go2 j = 
               if j < mA then 
                 let c = comp.Compare(a.[j],b.[j])
                 if c <> 0 then c else go2 (j+1) 
               else 0 
            go2 0

        let equalsV (comp: IEqualityComparer) (a:Vector<'T>) (b:Vector<'T>) = 
            let mA = a.NumRows
            let mB = b.NumRows 
            (mA = mB) && 
            let rec go2 j = (j >= mA) || (comp.Equals(a.[j],b.[j]) && go2 (j+1))
            go2 0

        let equalsRV (comp: IEqualityComparer) (a:RowVector<'T>) (b:RowVector<'T>) = 
            let mA = a.NumCols 
            let mB = b.NumCols 
            (mA = mB) && 
            let rec go2 j = (j >= mA) || (comp.Equals(a.[j],b.[j]) && go2 (j+1))
            go2 0

        let compareRV (comp: IComparer) (a:RowVector<'T>) (b:RowVector<'T>) = 
            let mA = a.NumCols 
            let mB = b.NumCols 
            let c = compare mA mB 
            if c <> 0 then c else
            let rec go2 j = 
               if j < mA then 
                 let c = comp.Compare(a.[j],b.[j])
                 if c <> 0 then c else go2 (j+1) 
               else 0 
            go2 0

        let inline combineHash x y = (x <<< 1) + y + 631 

        let hashM (comp:IEqualityComparer) (a:Matrix<_>) = 
            let nA = a.NumCols 
            let mA = a.NumRows 
            let acc = hash mA + hash nA
            a |> nonZeroEntriesM |> Seq.truncate 20 |> Seq.fold (fun z v -> combineHash z (comp.GetHashCode v)) acc
          
        let hashV (comp:IEqualityComparer) (a:Vector<_>) = 
            let mA = a.NumRows 
            hash mA +
            (let mutable c = 0 
             for i = 0 to mA - 1 do
                 c <- combineHash c (comp.GetHashCode a.[i])
             c)
          
        let hashRV (comp:IEqualityComparer) (a:RowVector<_>) = 
            let mA = a.NumCols 
            hash mA +
            (let mutable c = 0 
             for i = 0 to mA - 1 do
                 c <- combineHash c (comp.GetHashCode a.[i])
             c)
          
        type range = int * int

        let startR ((a,_) : range)   = a
        let countR ((a,b) : range)   = (b-a)+1
        let idxR    ((a,_) : range) i = a+i
        let inR    ((a,b) : range) i = a <= i && i <= b
        ///Returns row of index i of matrix a as a vector
        let getRowM  (a:Matrix<_>) i = createRVx (opsM a) a.NumCols (fun j -> a.[i,j])
        ///Replaces row of index j of matrix a with values of vector v, if vector length matches rowsize
        let setRowM (a:Matrix<_>) i (v:Vector<_>) = 
            if a.NumCols = v.Length then
                let l = v.Length-1
                for j = 0 to l do
                    a.[i,j] <- v.[j]
            elif a.NumCols < v.Length then
                failwith ("Can't set row, vector is longer than matrix column number")
            else 
                failwith ("Can't set row, vector is shorter than matrix column number")
        ///Returns col of index i of matrix a as a vector
        let getColM  (a:Matrix<_>) j = createVx (opsM a) a.NumRows (fun i -> a.[i,j])
        ///Replaces column of index i of matrix a with values of vector v, if vector length matches columnsize
        let setColM (a:Matrix<_>) j (v:Vector<_>) = 
            if a.NumCols = v.Length then
                let l = v.Length-1
                for i = 0 to l do
                    a.[i,j] <- v.[i]
            elif a.NumCols < v.Length then
                failwith ("Can't set column, vector is longer than matrix row number")
            else 
                failwith ("Can't set column, vector is shorter than matrix row number")
        let getRegionV  (a:Vector<_>)    r      = createVx a.OpsData (countR r) (fun i -> a.[idxR r i]) 
        let getRegionRV (a:RowVector<_>) r      = createRVx a.OpsData (countR r) (fun i -> a.[idxR r i]) 

        let getRegionM  a ri rj    = 
            match a with 
            | DenseRepr a -> createMx a.OpsData (countR ri) (countR rj) (fun i j -> a.[idxR ri i, idxR rj j]) 
            | _ -> nonZeroEntriesM a 
                   |> Seq.filter (fun (i,j,_) -> inR ri i && inR rj j) 
                   |> Seq.map (fun (i,j,v) -> (i-startR ri,j-startR rj,v)) 
                   |> initSparseM (countR ri) (countR rj)

        let getColsM (a:Matrix<_>) rj         = getRegionM a (0,a.NumRows - 1) rj
        let getRowsM (a:Matrix<_>) ri         = getRegionM a ri (0,a.NumCols - 1)

        let rowvecM (x:RowVector<_>) = initM 1         x.NumCols (fun _ j -> x.[j]) 
        let vectorM (x:Vector<_>) = initM x.NumRows  1         (fun i _ -> x.[i])  
        let toVectorM x = getColM x 0 
        let toRowVectorM x = getRowM x 0 
        let toScalarM (x:Matrix<_>) = x.[0,0]



//----------------------------------------------------------------------------
// type Matrix<'T> augmentation 
//--------------------------------------------------------------------------*)
// Interface implementation

    type Matrix<'T> with
        static member ( +  )(a: Matrix<'T>,b) = SpecializedGenericImpl.addM a b
        static member ( -  )(a: Matrix<'T>,b) = SpecializedGenericImpl.subM a b
        static member ( *  )(a: Matrix<'T>,b) = SpecializedGenericImpl.mulM a b
        static member ( *  )(a: Matrix<'T>,b : Vector<'T>) = SpecializedGenericImpl.mulMV a b

        static member ( * )((m: Matrix<'T>),k : 'T) = SpecializedGenericImpl.scaleM k m

        static member ( .* )(a: Matrix<'T>,b) = SpecializedGenericImpl.cptMulM a b
        static member ( * )(k,m: Matrix<'T>) = SpecializedGenericImpl.scaleM k m
        static member ( ~- )(m: Matrix<'T>)     = SpecializedGenericImpl.negM m
        static member ( ~+ )(m: Matrix<'T>)     = m
        // add +
        static member ( +  )(a: Matrix<'T>,k: 'T) = SpecializedGenericImpl.addScalarM k a
        static member ( +  )(k: 'T,a: Matrix<'T>) = SpecializedGenericImpl.addScalarM k a
        // sub -
        static member ( -  )(a: Matrix<'T>,k: 'T) = SpecializedGenericImpl.subScalarM k a
        static member ( -  )(k: 'T,a: Matrix<'T>) = SpecializedGenericImpl.subScalarM k a

        member m.GetSlice (start1,finish1,start2,finish2) = 
            let start1 = match start1 with None -> 0 | Some v -> v 
            let finish1 = match finish1 with None -> m.NumRows - 1 | Some v -> v 
            let start2 = match start2 with None -> 0 | Some v -> v 
            let finish2 = match finish2 with None -> m.NumCols - 1 | Some v -> v 
            SpecializedGenericImpl.getRegionM m (start1,finish1) (start2,finish2)

        member m.SetSlice (start1,finish1,start2,finish2,vs:Matrix<_>) = 
            let start1 = match start1 with None -> 0 | Some v -> v 
            let finish1 = match finish1 with None -> m.NumRows - 1 | Some v -> v 
            let start2 = match start2 with None -> 0 | Some v -> v 
            let finish2 = match finish2 with None -> m.NumCols - 1 | Some v -> v 
            for i = start1 to finish1  do 
                for j = start2 to finish2 do
                    m.[i,j] <- vs.[i-start1,j-start2]

        /// RowCount * ColumnCount
        member m.Dimensions = m.NumRows,m.NumCols

        member m.Transpose = SpecializedGenericImpl.transM m
        member m.PermuteRows (p: permutation) : Matrix<'T> = SpecializedGenericImpl.permuteRows p m
        member m.PermuteColumns (p: permutation) : Matrix<'T> = SpecializedGenericImpl.permuteColumns p m


// Interface implementation
        
        interface IEnumerable<'T> with 
            member m.GetEnumerator() = 
               (seq { for i in 0 .. m.NumRows-1 do
                        for j in 0 .. m.NumCols - 1 do 
                            yield m.[i,j] }).GetEnumerator()

        interface IEnumerable with 
            member m.GetEnumerator() =  ((m :> IEnumerable<_>).GetEnumerator() :> IEnumerator)
                                    
        interface System.IComparable with 
             member m.CompareTo(yobj:obj) = SpecializedGenericImpl.compareM LanguagePrimitives.GenericComparer m (yobj :?> Matrix<'T>)
             
        interface IStructuralComparable with
            member m.CompareTo(yobj:obj,comp:System.Collections.IComparer) = SpecializedGenericImpl.compareM comp m (yobj :?> Matrix<'T>)
            
        override m.GetHashCode() = SpecializedGenericImpl.hashM LanguagePrimitives.GenericEqualityComparer m 
        override m.Equals(yobj:obj) = 
            match yobj with 
            | :? Matrix<'T> as m2 -> SpecializedGenericImpl.equalsM LanguagePrimitives.GenericEqualityComparer m m2
            | _ -> false
        
        interface IStructuralEquatable with
            member m.GetHashCode(comp:System.Collections.IEqualityComparer) = SpecializedGenericImpl.hashM comp m
            member m.Equals(yobj:obj,comp:System.Collections.IEqualityComparer) = 
                match yobj with 
                | :? Matrix<'T> as m2 -> SpecializedGenericImpl.equalsM comp m m2
                | _ -> false

        /// Returns four chunks of the matrix as strings depending on the respective row/column start/end count, separated by a row and column indicating omitted rows and columns.
        member m.FormatStrings(rowStartCount, rowEndCount, columnStartCount, columnEndCount) =
            let nRows, nCols = m.Dimensions
            let displayRows = rowStartCount + rowEndCount
            let displayCols = columnStartCount + columnEndCount


            if displayRows >= nRows && displayCols >= nCols then // this formats the full matrix without omitted rows/cols
                Array.init (nRows+2) (fun rowIndex ->
                    match rowIndex with 
                    | 0 -> [|"";"";yield! [for i = 0 to nCols-1 do yield string i]|] // column index header row
                    | 1 -> [|for i in 0 .. nCols+1 do yield ""|] // empty row to distinguish column indices from data in FSI/StructuredDisplay
                    | _ -> // the rest of the rows contain data
                        Array.init (nCols+2) (fun colIndex ->
                            match colIndex with
                            | 0 -> string (rowIndex-2) // the first column index contains the row index
                            | 1 -> "->" // the second column index contains a separator to distinguish row indices from data in FSI/StructuredDisplay
                            | _ -> m.[rowIndex-2,colIndex-2] |> formatValue // the rest is data. sswitches below work the same, only ommiting some of the data.
                        )
                )
            elif displayRows >= nRows && displayCols < nCols then // this formats the matrix with only ommitted cols
                Array.init (nRows+2) (fun rowIndex ->
                    match rowIndex with
                    | 0 -> // column index header row
                        [|
                            "";"";
                            yield! [for i in [0 .. columnStartCount-1] do yield string i]; 
                            "..."; 
                            yield! [for i in [nCols - columnEndCount .. nCols - 1] do yield string i]
                        |]
                    | 1 -> [|for i in 0 .. (displayCols + 2) do yield ""|] // empty row to distinguish column indices from data in FSI/StructuredDisplay
                    | _ -> // the rest of the rows contain data
                        Array.init (columnStartCount+columnEndCount+3) (fun colIndex ->
                            if (colIndex-2) < columnStartCount then // left
                                match colIndex with
                                | 0 -> string (rowIndex-2) 
                                | 1 -> "->"
                                | _ -> m.[rowIndex-2,colIndex-2] |> formatValue
                            elif (colIndex-2) > columnStartCount then // right
                                m[rowIndex-2,(nCols - 3 - columnEndCount + colIndex - columnStartCount)] |> formatValue
                            else 
                                "..." // separator for signalling ommitted cols
                        )
                )
            elif displayRows < nRows && displayCols >= nCols then // this formats the matrix with only ommitted rows
                Array.init (rowStartCount+rowEndCount+3) (fun rowIndex ->
                    match rowIndex with
                    | 0 -> [|"";"";yield! [for i = 0 to nCols-1 do yield string i]|] // column index header row
                    | 1 -> [|for i in 0 .. nCols+1 do yield ""|] // empty row to distinguish column indices from data in FSI/StructuredDisplay
                    | _ ->
                        Array.init (nCols+2) (fun colIndex ->
                            if (rowIndex-2) < rowStartCount then // upper half
                                match colIndex with
                                | 0 -> string (rowIndex-2)
                                | 1 -> "->"
                                | _ -> m.[rowIndex-2,colIndex-2] |> formatValue
                            elif (rowIndex-2) > rowStartCount then // lower half
                                match colIndex with
                                | 0 -> string (nRows - 3 - rowEndCount + rowIndex - rowStartCount)
                                | 1 -> "->"
                                | _ -> m[(nRows - 3 - rowEndCount + rowIndex - rowStartCount),colIndex - 2] |> formatValue
                            else 
                                match colIndex with // separator for signalling ommitted rows
                                | 0 -> ":"
                                | 1 -> ""
                                | _ -> "..."
                        )           
                    )
            else // this formats the matrix with ommitted rows and cols
                Array.init (rowStartCount+rowEndCount+3) (fun rowIndex -> 
                    match rowIndex with
                    | 0 -> // column index header row
                        [|
                            "";"";
                            yield! [for i in [0 .. columnStartCount-1] do yield string i]; 
                            "..."; 
                            yield! [for i in [nCols - columnEndCount .. nCols - 1] do yield string i]
                        |]
                    | 1 -> [|for i in 0 .. (displayCols + 2) do yield ""|] // empty row to distinguish column indices from data in FSI/StructuredDisplay
                    | _ -> 
                        Array.init (columnStartCount+columnEndCount+3) (fun colIndex ->
                            if (rowIndex-2) < rowStartCount then // upper half
                                if (colIndex-2) < columnStartCount then // upper left
                                    match colIndex with
                                    | 0 -> string (rowIndex-2) 
                                    | 1 -> "->"
                                    | _ -> m.[rowIndex-2,colIndex-2] |> formatValue
                                elif (colIndex-2) > columnStartCount then // upper right
                                    m[rowIndex-2,(nCols - 3 - columnEndCount + colIndex - columnStartCount)] |> formatValue
                                else 
                                    "..."
                            elif (rowIndex-2) > rowStartCount then // lower half
                                if (colIndex-2) < columnStartCount then // lower left
                                    match colIndex with
                                    | 0 -> string (nRows - 3 - rowEndCount + rowIndex - rowStartCount)
                                    | 1 -> "->"
                                    | _ -> m[(nRows - 3 - rowEndCount + rowIndex - rowStartCount),(colIndex-2)] |> formatValue
                                elif (colIndex-2) > columnStartCount then // lower right
                                    m[(nRows - 3 - rowEndCount + rowIndex - rowStartCount),(nCols - 3 - columnEndCount + colIndex - columnStartCount)] |> formatValue
                                else 
                                    "..."
                            else 
                                match colIndex with
                                | 0 -> ":"
                                | 1 -> ""
                                | _ -> "..."
                        )           
                    )

        member m.Format(rowStartCount, rowEndCount, columnStartCount, columnEndCount, showInfo) =
            try
                let formattedtable =
                    m.FormatStrings(rowStartCount, rowEndCount, columnStartCount, columnEndCount)
                    |> array2D
                    |> Formatting.formatTable
                if showInfo then
                    let matrixInfo = sprintf "Matrix of %i rows x %i columns" m.NumRows m.NumCols
                    sprintf "%s%s%s" formattedtable System.Environment.NewLine matrixInfo
                else
                    formattedtable
            with e -> sprintf "Formatting failed: %A" e

        member m.Format(rowCount, columnCount, showInfo:bool) =
            let rowHalf = rowCount / 2
            let colHalf = columnCount / 2
            m.Format(rowHalf, rowHalf, colHalf, colHalf, showInfo)

        member m.Format(showInfo:bool) =
            m.Format(
                Formatting.Matrix.RowStartItemCount, 
                Formatting.Matrix.RowEndItemCount, 
                Formatting.Matrix.ColumnStartItemCount, 
                Formatting.Matrix.ColumnEndItemCount,
                showInfo
            )

        interface IFsiFormattable with
            member m.Format() = m.Format(false)
            member m.FormatWithInfo() = m.Format(true)

        interface IMatrixFormattable with
            member m.InteractiveFormat(rowCount,colCount) =
                m.FormatStrings(
                    (rowCount / 2),
                    (rowCount / 2),
                    (colCount / 2),
                    (colCount / 2)
                )
            member m.GetNumRows() = m.NumRows
            member m.GetNumCols() = m.NumCols

        override m.ToString() = 
            match m with 
            | DenseRepr m -> GenericImpl.showDenseMatrixGU m
            | SparseRepr _ -> "<sparse>"

        member m.DebugDisplay = 
            let txt = 
                match m with 
                | DenseRepr m -> GenericImpl.debugShowDenseMatrixGU m
                | SparseRepr _ -> "<sparse>"
            new System.Text.StringBuilder(txt)  // return an object with a ToString with the right value, rather than a string. (strings get shown using quotes)

        member m.StructuredDisplayAsFormattedMatrix =
            sprintf "%s%s" System.Environment.NewLine (m.Format(true))

        member m.StructuredDisplayAsArray =  
            let rec layout m = 
                match m with 
                | DenseRepr _ -> box (Array2D.init m.NumRows m.NumCols (fun i j -> m.[i,j]))
                | SparseRepr _ -> (if m.NumRows < 20 && m.NumCols < 20 then layout (SpecializedGenericImpl.toDenseM m) else box(SpecializedGenericImpl.nonZeroEntriesM m))
            layout m



//----------------------------------------------------------------------------
// type Vector<'T> augmentation
//--------------------------------------------------------------------------*)
// Interface implementation

    type Vector<'T> with
        static member ( +  )(a: Vector<'T>,b) = SpecializedGenericImpl.addV a b
        static member ( -  )(a: Vector<'T>,b) = SpecializedGenericImpl.subV a b
        static member ( .* )(a: Vector<'T>,b) = SpecializedGenericImpl.cptMulV a b
        
        static member ( * )(k,m: Vector<'T>) = SpecializedGenericImpl.scaleV k m
        
        static member ( * )(a: Vector<'T>,b) = SpecializedGenericImpl.mulVRV a b
        
        static member ( * )(m: Vector<'T>,k) = SpecializedGenericImpl.scaleV k m
        
        static member ( ~- )(m: Vector<'T>)     = SpecializedGenericImpl.negV m
        static member ( ~+ )(m: Vector<'T>)     = m

        // add +
        static member ( + )(k,m: Vector<'T>) = SpecializedGenericImpl.addScalarV k m
        static member ( + )(m: Vector<'T>,k) = SpecializedGenericImpl.addScalarV k m
        // sub -
        static member ( - )(k,m: Vector<'T>) = SpecializedGenericImpl.subScalarV k m
        static member ( - )(m: Vector<'T>,k) = SpecializedGenericImpl.subScalarV k m

        member m.GetSlice (start,finish) = 
            let start = match start with None -> 0 | Some v -> v 
            let finish = match finish with None -> m.NumRows - 1 | Some v -> v 
            SpecializedGenericImpl.getRegionV m (start,finish)

        member m.SetSlice (start,finish,vs:Vector<_>) = 
            let start = match start with None -> 0 | Some v -> v 
            let finish = match finish with None -> m.NumRows - 1 | Some v -> v 
            for i = start to finish  do 
                    m.[i] <- vs.[i-start]


        member m.DebugDisplay = 
            let txt = GenericImpl.showVecGU "vector" m
            new System.Text.StringBuilder(txt)  // return an object with a ToString with the right value, rather than a string. (strings get shown using quotes)

        member m.StructuredDisplayAsArray =  Array.init m.NumRows (fun i -> m.[i])

        member m.Details = m.Values

        member m.Transpose = SpecializedGenericImpl.transV m
        
        member m.Permute (p:permutation) = SpecializedGenericImpl.permuteV p m

      
        interface System.IComparable with 
             member m.CompareTo(y:obj) = SpecializedGenericImpl.compareV LanguagePrimitives.GenericComparer m (y :?> Vector<'T>)
        
        interface IStructuralComparable with
            member m.CompareTo(y:obj,comp:System.Collections.IComparer) = SpecializedGenericImpl.compareV comp m (y :?> Vector<'T>)

        interface IStructuralEquatable with
            member x.GetHashCode(comp) = SpecializedGenericImpl.hashV comp x
            member x.Equals(yobj,comp) = 
                match yobj with 
                | :? Vector<'T> as v2 -> SpecializedGenericImpl.equalsV comp x v2
                | _ -> false

        override x.GetHashCode() = 
            SpecializedGenericImpl.hashV LanguagePrimitives.GenericEqualityComparer x

        override x.Equals(yobj) = 
            match yobj with 
            | :? Vector<'T> as v2 -> SpecializedGenericImpl.equalsV LanguagePrimitives.GenericEqualityComparer x v2
            | _ -> false

        override m.ToString() = GenericImpl.showVecGU "vector" m

//----------------------------------------------------------------------------
// type RowVector<'T> augmentation
//--------------------------------------------------------------------------*)

    type RowVector<'T> with
        static member ( +  )(a: RowVector<'T>,b) = SpecializedGenericImpl.addRV a b
        static member ( -  )(a: RowVector<'T>,b) = SpecializedGenericImpl.subRV a b
        static member ( .* )(a: RowVector<'T>,b) = SpecializedGenericImpl.cptMulRV a b
        static member ( * )(k,v: RowVector<'T>) = SpecializedGenericImpl.scaleRV k v
        
        static member ( * )(a: RowVector<'T>,b: Matrix<'T>) = SpecializedGenericImpl.mulRVM a b
        static member ( * )(a: RowVector<'T>,b:Vector<'T>) = SpecializedGenericImpl.mulRVV a b
        static member ( * )(v: RowVector<'T>,k:'T) = SpecializedGenericImpl.scaleRV k v
        
        static member ( ~- )(v: RowVector<'T>)     = SpecializedGenericImpl.negRV v
        static member ( ~+ )(v: RowVector<'T>)     = v

        // add +
        static member ( + )(v: RowVector<'T>,k:'T) = SpecializedGenericImpl.addScalarRV k v
        static member ( + )(k:'T,v: RowVector<'T>) = SpecializedGenericImpl.addScalarRV k v
        // sub -
        static member ( - )(v: RowVector<'T>,k:'T) = SpecializedGenericImpl.subScalarRV k v
        static member ( - )(k:'T,v: RowVector<'T>) = SpecializedGenericImpl.subScalarRV k v
        
        member m.GetSlice (start,finish) = 
            let start = match start with None -> 0 | Some v -> v
            let finish = match finish with None -> m.NumCols - 1 | Some v -> v 
            SpecializedGenericImpl.getRegionRV m (start,finish)

        member m.SetSlice (start,finish,vs:RowVector<_>) = 
            let start = match start with None -> 0 | Some v -> v 
            let finish = match finish with None -> m.NumCols - 1 | Some v -> v 
            for i = start to finish  do 
                   m.[i] <- vs.[i-start]


        member m.DebugDisplay = 
            let txt = GenericImpl.showRowVecGU "rowvec" m
            new System.Text.StringBuilder(txt)  // return an object with a ToString with the right value, rather than a string. (strings get shown using quotes)

        member m.StructuredDisplayAsArray =  Array.init m.NumCols (fun i -> m.[i])

        member m.Details = m.Values

        member m.Transpose = SpecializedGenericImpl.transRV m
        
        member m.Permute (p:permutation) = SpecializedGenericImpl.permuteRV p m

        override m.ToString() = GenericImpl.showRowVecGU "rowvec" m
     
        interface System.IComparable with 
            member m.CompareTo(y) = SpecializedGenericImpl.compareRV LanguagePrimitives.GenericComparer m (y :?> RowVector<'T>)
        
        interface IStructuralComparable with
            member m.CompareTo(y,comp) = SpecializedGenericImpl.compareRV comp m (y :?> RowVector<'T>)

        interface IStructuralEquatable with
            member x.GetHashCode(comp) = SpecializedGenericImpl.hashRV comp x
            member x.Equals(yobj,comp) = 
                match yobj with 
                | :? RowVector<'T> as rv2 -> SpecializedGenericImpl.equalsRV comp x rv2
                | _ -> false

        override x.GetHashCode() = 
            SpecializedGenericImpl.hashRV LanguagePrimitives.GenericEqualityComparer x

        override x.Equals(yobj) = 
            match yobj with 
            | :? RowVector<'T> as rv2 -> SpecializedGenericImpl.equalsRV LanguagePrimitives.GenericEqualityComparer x rv2
            | _ -> false





    type matrix = Matrix<float>
    type vector = Vector<float>
    type rowvec = RowVector<float>




//    type Matrix<'T> with 
//        member x.ToArray2()        = Matrix.Generic.toArray2D x
//        member x.ToArray2D()        = Matrix.Generic.toArray2D x
//
//#if FX_NO_DEBUG_DISPLAYS
//#else
//        [<DebuggerBrowsable(DebuggerBrowsableState.Collapsed)>]
//#endif
//
//        member x.NonZeroEntries    = Matrix.Generic.nonzero_entries x
//        member x.ToScalar()        = Matrix.Generic.toScalar x
//        member x.ToRowVector()     = Matrix.Generic.toRowVector x               
//        member x.ToVector()        = Matrix.Generic.toVector x
//
//#if FX_NO_DEBUG_DISPLAYS
//#else
//        [<DebuggerBrowsable(DebuggerBrowsableState.Collapsed)>]
//#endif
//        member x.Norm              = Matrix.Generic.norm x
//
//        member x.Column(n)         = Matrix.Generic.getCol x n
//        member x.Row(n)            = Matrix.Generic.getRow x n
//        member x.Columns (i,ni)    = Matrix.Generic.getCols x i ni
//        member x.Rows (j,nj)       = Matrix.Generic.getRows x j nj
//        member x.Region(i,j,ni,nj) = Matrix.Generic.getRegion x i j ni nj
//        member x.GetDiagonal(i)    = Matrix.Generic.getDiagN x i
//
//#if FX_NO_DEBUG_DISPLAYS
//#else
//        [<DebuggerBrowsable(DebuggerBrowsableState.Collapsed)>]
//#endif
//        member x.Diagonal          = Matrix.Generic.getDiag x
//
//        member x.Copy () = Matrix.Generic.copy x
//
//
//    type Vector<'T> with 
//        member x.ToArray() = Vector.Generic.toArray x
//        member x.Norm      = Vector.Generic.norm x
//        member x.Copy ()   = Vector.Generic.copy x
//
//
//    type RowVector<'T> with 
//        member x.ToArray() = RowVector.Generic.toArray x
//        member x.Copy ()   = RowVector.Generic.copy x
//
//    [<AutoOpen>]
//    module MatrixTopLevelOperators = 
//
//        let matrix ll = Matrix.ofSeq ll
//        let vector l  = Vector.ofSeq  l
//        let rowvec l  = RowVector.ofSeq l


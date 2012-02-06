
open System
open System.IO
open System.Text
open Neo4jClient
open ServiceStack.Redis

//use union or record
//type MyNode() =
//    let mutable myInternalValue = ""
//    member this.MyProperty
//         with get() = myInternalValue
//         and set(value) = myInternalValue <- value

[<Literal>]
let MaxDepth = 10
//let mutable sb = new StringBuilder()
let redisClient = new RedisClient("192.168.1.120")

let printDetails (dir:String) depth = 
    let depthSeq = {2..depth}
    let sb = new StringBuilder()
    //Seq.iter (fun tab -> sb.Append("--") |> ignore) depthSeq
    Seq.iter (fun tab -> sb.Append("\t") |> ignore) depthSeq
    //sb.Append "|--" |> ignore
    sb.Append dir  |> ignore
   // sb.Append " " |> ignore
   // sb.Append depth  |> ignore
    sb.Append("\n")  |> ignore
    Console.WriteLine(sb.ToString())

let recordFileSize (file:FileInfo) name=
             match redisClient.AddItemToSortedSet("size", name, file.Length) with 
                        | false -> Console.WriteLine "Problem with writing file size to Redis"
                        | true -> ()

let processFiles (files:FileInfo[]) depth = 
   files |> Array.toSeq 
         |> Seq.iter (
            fun file -> let name = file.ToString()
                        printDetails (name) depth
                        recordFileSize file name
                        
                        //add file size into sorted set
                       
                        //ZAdd("size", size , name.t) 
                        //()
                        )    


let rec recursiveDir (dir:DirectoryInfo) (depth:int) = 
    match depth  with 
        | MaxDepth -> Console.WriteLine "Max Depth Reached"
        | _ ->
            let children = dir.GetDirectories() 
            let path = dir.ToString()
            match children.Length with 
                | x when x =  0 ->  
                       printDetails path depth 
                       processFiles (dir.GetFiles()) depth
                | _ -> printDetails path depth
                       Array.toSeq children
                        |> Seq.iter (fun x -> recursiveDir x (depth + 1))
            
            //Console.WriteLine(tw.ToString())


let start = new DirectoryInfo(@"C:\Users\forthold\Downloads")
recursiveDir start 0
//
//let addOne b = 1 + b
//addOne 21 |> ignore
//let b = addOne 12
//addOne b |> ignore
//let uri = new Uri "http://192.168.1.112:7474/db/data"
//let client = new GraphClient(uri)
//client.Connect() 
//let node = new MyNode()
//node.MyProperty <- "foo"
//let nodeRef = client.Create node


Console.ReadLine() |> ignore
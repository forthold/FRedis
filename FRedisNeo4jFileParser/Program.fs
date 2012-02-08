
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
    
let constructKey (objectType:String) (field:String) (id:String) = 
    let a = new StringBuilder()
    a.Append(objectType) |> ignore
    a.Append(":") |> ignore
    a.Append(id) |> ignore
    a.Append(":") |> ignore
    a.Append(field) |> ignore
    a.ToString()

let recordFileSize (length:int64) id=
     let result = redisClient.AddItemToSortedSet("files.by.size", id, length) 
     match result with
        | false -> Console.WriteLine "Problem with writing file size to Redis: is set value not unique?"
        | true -> ()

let processFiles (files:FileInfo[]) depth directoryId= 
    
    Array.fold (fun folderSize (file:FileInfo) ->  
                        printDetails (file.ToString()) depth
                        let nextFileId = Convert.ToString (redisClient.Incr "next.file.id" )
                        redisClient.Set ((constructKey "file" "name" nextFileId), file.Name ) |> ignore
                        redisClient.Set ((constructKey "file" "path" nextFileId), file.DirectoryName ) |> ignore
                        redisClient.Set ((constructKey "file" "extention" nextFileId), file.Extension) |> ignore
                        redisClient.AddItemToSet ( ( constructKey "dir" "files" directoryId ) , nextFileId )
                        recordFileSize file.Length (nextFileId.ToString())
                        redisClient.Set ((constructKey "file" "size" nextFileId), file.Length ) |> ignore
                        folderSize + Convert.ToInt32 file.Length                            
                                ) 0 files
            //   files |> Array.toSeq 
//         |> Seq.iter (
//            fun file -> let name = file.ToString()
//                        printDetails (name) depth
//                        recordFileSize file name
//                        
//                        //add file size into sorted set
//                       
//                        //ZAdd("size", size , name.t) 
//                        //()
//                        )    

// Get children directories and call recursivly until max depth reached
// Get an new id for each directory and store name
// Process files if there are not directories returning folder size
let rec recursiveDir (dir:DirectoryInfo) (depth:int) parentDirectoryId = 
    match depth  with 
        | MaxDepth -> Console.WriteLine "Max Depth Reached"
        | _ ->
            let children = dir.GetDirectories() 
            //redisClient.Incr "next.directory.id" |> constructKey "dir" "path"
            let nextDirId = Convert.ToString (redisClient.Incr "next.directory.id" )
            redisClient.Set ((constructKey "dir" "path" nextDirId), dir.ToString()) |> ignore
            match parentDirectoryId with 
                | "" -> ()
                | _ ->  redisClient.AddItemToSet( (constructKey "dir" "directories" parentDirectoryId), nextDirId )
            //let path = dir.ToString()
            match children.Length with 
                | x when x =  0 ->  
                       printDetails (dir.ToString()) depth 
                       redisClient.Set ((constructKey "dir" "size" nextDirId), (processFiles (dir.GetFiles()) depth nextDirId)) |> ignore
                | _ -> printDetails (dir.ToString()) depth
                       Array.toSeq children
                        |> Seq.iter (fun x -> recursiveDir x (depth + 1) nextDirId)
            
            //Console.WriteLine(tw.ToString())


let start = new DirectoryInfo(@"C:\Users\forthold\Downloads")
recursiveDir start 0 ""
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
open Calc.Lexer
open Calc.Parser

[<Literal>]
let traceStep = 20000L

type lexer_reply = token

let private _buildAst srcFilePath =     
    use istream = new System.IO.FileStream(srcFilePath
                                        , System.IO.FileMode.Open
                                        , System.IO.FileAccess.Read
                                        , System.IO.FileShare.Read
                                        , 100 <<< 20
                                        , System.IO.FileOptions.SequentialScan 
                                        )
    use sr = new System.IO.StreamReader(istream, System.Text.Encoding.GetEncoding 1251)
    let lastTokenNum = ref 0L
    let tokenizerFun = 
            let lexbuf = Lexing.LexBuffer<_>.FromTextReader sr                        
            let timeOfIteration = ref System.DateTime.Now
            fun (chan:MailboxProcessor<lexer_reply>) ->
            let post = chan.Post 
            async {
                while not lexbuf.IsPastEndOfStream do
                  lastTokenNum := 1L + !lastTokenNum
                  if (!lastTokenNum % traceStep) = 0L then 
                     let oldTime = !timeOfIteration
                     timeOfIteration := System.DateTime.Now
                     let mSeconds = int64 ((!timeOfIteration - oldTime).Duration().TotalMilliseconds)
                     printfn "tkn# %10d Tkns/s:%8d - l" lastTokenNum.Value (1000L * traceStep/ mSeconds)                    
                     if int64 chan.CurrentQueueLength > 2L * traceStep then                                                                                  
                            int (int64 chan.CurrentQueueLength * mSeconds / traceStep)  |> System.Threading.Thread.Sleep      
                  let tok = Calc.Lexer.token lexbuf                
                  //printfn "TOK:%A" tok 
                  // Process tokens. Filter comments. Add some context-depenede information.
                  post tok
            }   

    use tokenizer =  new MailboxProcessor<_>(tokenizerFun)
    let i = ref 0L    

    let timeOfIteration = ref System.DateTime.Now        
    
    tokenizer.Error.Add (fun exn -> printfn "Lexer failure: %A" exn.Message )
             
    let getNextToken (lexbuf:Lexing.LexBuffer<_>) =
        let res = tokenizer.Receive 150000 |> Async.RunSynchronously
        i := 1L + !i 

        if (!i % traceStep) = 0L then 
          let oldTime = !timeOfIteration
          timeOfIteration := System.DateTime.Now
          let seconds = (!timeOfIteration - oldTime).TotalSeconds
          printfn "tkn# %10d Tkns/s:%8.0f - p" i.Value (float traceStep / seconds)
        res

    try 
      let res =         
        printfn "Lexer input: %s\n" (System.IO.Path.GetFullPath srcFilePath)
        let start = System.DateTime.Now
        tokenizer.Start()            
        let res = Calc.Parser.file getNextToken <| Lexing.LexBuffer<_>.FromString "*this is stub*"
        printfn "Time = %A" (System.DateTime.Now-start).TotalSeconds
        res
      res
    with e ->                 
        let extendedMessage = sprintf "Parse error.\ntkn#:%d\nexn: %A" !i e
        failwith extendedMessage

let buildAst srcFile =    
    let ast = _buildAst srcFile
    ast |> printfn "%A"

do buildAst @"..\..\input.txt"
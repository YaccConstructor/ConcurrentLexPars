ConcurrentLexPars
=================

Lexing and parsing concurrently in F# in fslex and fsyacc

=================
Core of idea. You can run lexer in mailBoxProcessor. Lexer should produce new tokens, process and post them. Lexer often faster than parser, and sometimes it should wait for a parser. Parser can receive next token when necessary. Code provided below. You can modify timeouts, traceStep to find optimal for your solution.

```

[<Literal>]
let traceStep = 200000L

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
                if int64 chan.CurrentQueueLength > 2L * traceStep then                                                                                  
                    int (int64 chan.CurrentQueueLength * mSeconds / traceStep)  |> System.Threading.Thread.Sleep      
            let tok = Calc.Lexer.token lexbuf
            // Process tokens. Filter comments. Add some context-depenede information.
            post tok
    }   

use tokenizer =  new MailboxProcessor<_>(tokenizerFun)

let getNextToken (lexbuf:Lexing.LexBuffer<_>) =
    let res = tokenizer.Receive 150000 |> Async.RunSynchronously
    i := 1L + !i 

    if (!i % traceStep) = 0L then 
        let oldTime = !timeOfIteration
        timeOfIteration := System.DateTime.Now
        let seconds = (!timeOfIteration - oldTime).TotalSeconds          
    res

let res =         
    tokenizer.Start()            
    Calc.Parser.file getNextToken <| Lexing.LexBuffer<_>.FromString "*this is stub*"
    
```

Full solution is available here: https://github.com/YaccConstructor/ConcurrentLexPars In this solution we only demonstrate full implementation of described idea . Performance comparison is not actual because semantic calculation is very simple and no tokens processing.

To find out performance comparison result look at full report https://docs.google.com/document/d/1K43g5jokNKFOEHQJVlHM1gVhZZ7vFK2g9CJHyAVtUtg/edit?usp=sharing Here we compare performance of sequential and concurrent solution for parser of T-SQL subset. Sequential: 27 sec, concurrent: 20 sec.

Also we use this technique in production T-SQL translator.

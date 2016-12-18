﻿module QuizPresentator.Logic

type QuestionString = Question of string
type AnswerString = string
type AnswerIndex = | A | B | C | D

let indexFromString string =
    match string with
    | "A" | "a" | "1" -> A
    | "B" | "b" | "2" -> B
    | "C" | "c" | "3" -> C
    | "D" | "d" | "4" -> D
    | _ -> failwith "no valid string from AnswerIndex"

// type Joker = 
type Question = {question: QuestionString; answers: AnswerString * AnswerString * AnswerString * AnswerString; correct: AnswerIndex; result: bool option}
type Party = {questions: Question list}
type public Quiz = {parties: Party list; activeParty: int}

// Creation
let private question question correct answerA answerB answerC answerD = {question = Question question; answers = (answerA, answerB, answerC, answerD); correct = correct; result = None}
let private emptyParty() =
    {questions = []}
let rec emptyQuiz parties =
    match parties with
    | 1 -> {parties = [emptyParty()]; activeParty = 0}
    | i when i > 1 ->
        let {parties = parties} = emptyQuiz (i-1)
        let parties' = List.append [emptyParty()] parties
        {parties = parties'; activeParty = 0}
    | _ -> invalidArg "parties" "Invalid partysize"
        
let private addQuestion party question =
    let {questions = questions} = party
    {party with questions = questions @ [question]}

// Simple Getter
let private getQuestionFromParty party =
    let {questions = questions} = party
    let fold res question =
        let {result = result} = question
        match res with
        | Some q -> Some q
        | None ->
            match result with
            | None -> Some question
            | Some _ -> None
    List.fold fold None questions

let private getQuestion quiz =
    let {parties = parties; activeParty = i} = quiz
    List.item i parties |> getQuestionFromParty

let private getAnswers quiz =
    match getQuestion quiz with
    | None -> ("", "", "", "")
    | Some {answers = answers} -> answers

// Getter for Gui
let getQuestionString quiz =
    match getQuestion quiz with
    | None -> ""
    | Some {question = Question question} -> question

let private getAnswerString answerIndex quiz =
    let (aA, aB, aC, aD) = getAnswers quiz
    match answerIndex with | A -> aA | B -> aB | C -> aC | D -> aD
let getAnswerA = getAnswerString A
let getAnswerB = getAnswerString B
let getAnswerC = getAnswerString C
let getAnswerD = getAnswerString D

let getLength {parties = parties} =
    List.fold (fun amount {questions = questions} -> amount + List.length questions) 0 parties

let getNrOfParties {parties = parties} = List.length parties

let rec private countResults quiz results offset =
    match results with
    | result::rest ->
        match offset with
        | 0 ->
            match result with
            | true -> 1 + countResults quiz rest ((getNrOfParties quiz) - 1)
            | false -> countResults quiz rest ((getNrOfParties quiz) - 1)
        | _ when offset < getNrOfParties quiz -> 
            countResults quiz rest (offset - 1)
        | _ -> failwith "Invalid Party index"
    | [] -> 0

let isEnded quiz =
    let {parties = parties} = quiz
    let fold ended party =
        if ended = true then
            match getQuestionFromParty party with
            | None -> true
            | Some _ -> false
        else
            ended
    List.fold fold true parties

// Flow
let private checkAnswer quiz answer =
    match getQuestion quiz with
    | None -> failwith "Quiz has no valid state. No question to choose an answer from!"
    | Some {question = _; answers = _; correct = correct} -> answer = correct

let updateResult party result =
    let {questions = questions} = party

    let mapFold res (question: Question) =
        match res with
        | None ->
            (question, res)
        | Some r ->
            match question.result with
            | Some _ ->
                (question, res)
            | None ->
                ({question with result = Some r}, None)
    let (out,_) = List.mapFold mapFold (Some result) questions
    out

let chooseAnswer quiz answer =
    let {parties=parties; activeParty=active} = quiz
    let party' = {(List.item active parties) with questions = updateResult (List.item active parties) (checkAnswer quiz answer)}
    let parties' = List.mapi (fun i party -> if i = active then party' else party) parties
    {quiz with parties = parties'; activeParty = (active+1) % (List.length parties)}

// Questions from file
let readLines filePath = System.IO.File.ReadLines(filePath) |> List.ofSeq

let getArgsFromLine (line : string) = line.Split ';' |> List.ofArray |> List.map (fun el -> el.Trim ' ')

let addQuestionFromLine (line : string) party =
    let args = getArgsFromLine line
    match args with
    | questionString::correct::answerA::answerB::answerC::[answerD] ->
        let index = indexFromString correct
        addQuestion party (question questionString index answerA answerB answerC answerD)
    | _ -> failwith "line has wrong number of arguments"

let rec addQuestionsFromLines index lines quiz =
    match lines with
    | line::rest ->
        let {parties = parties} = quiz
        let party = List.item index parties
        let party' = addQuestionFromLine line party
        let quiz' = {quiz with parties = List.mapi (fun i party -> if i = index then party' else party) parties}
        addQuestionsFromLines ((index + 1) % (List.length quiz'.parties)) rest quiz'
    | [] ->
        quiz
    
let initQuizFromFile filePath =
    let lines = readLines filePath
    match lines with
    | [] ->
        failwith "file is empty"
    | partiesString::questionLines ->
        match System.Int32.TryParse(partiesString) with
        // First line is a single integer
        | (true,parties) ->
            emptyQuiz parties |> addQuestionsFromLines 0 questionLines
        | _ ->
            emptyQuiz 1 |> addQuestionsFromLines 0 lines

// Define object functions for use in C#
type Quiz with
    member this.Question = getQuestionString this
    member this.AnswerA = getAnswerA this
    member this.AnswerB = getAnswerB this
    member this.AnswerC = getAnswerC this
    member this.AnswerD = getAnswerD this
    member this.Size = getLength this
    member this.Ended = isEnded this
    member this.NrOfParties = getNrOfParties this
    member this.CheckAnswer index = checkAnswer this index
    member this.ChooseAnswer index = chooseAnswer this index
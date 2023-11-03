﻿﻿using ChessChallenge.API;
using System;

public class MyBot : IChessBot
{  

    Move bestmoveRoot = Move.NullMove;

    // https://www.chessprogramming.org/PeSTO%27s_Evaluation_Function
    int[] pieceVal = {0, 100, 310, 330, 500, 900, 10000 };
    int[] knights = { -40, -10, -20, -20, -20, -20, -10, -40,
                            -40, -20,  0,  0,  0,  0, -20, -40,
                            -30,  10, 10, 15, 15, 10,  10, -30,
                            -30,  5, 15, 20, 20, 15,  5, -30,
                            -30,  0, 15, 20, 20, 15,  0, -30,
                            -30,  0, 25, 25, 25, 25,  0, -30,
                            -40, -20,  10,  15,  15,  10, -20, -40,
                            -40, -30, -20, -10, -10, -20, -30, -40 };

            int[] bishops = { -20, -10, -10, -10, -10, -10, -10, -20,
                            10, 25, 0, 0, 0, 0, 25, 10,
                            5, 0, 5, 15, 15, 5, 0, 5,
                            -10, 25, 30, 10, 10, 30, 25, -10,
                            -10, 5, 10, 10, 10, 10, 5, -10,
                            15, 10, 10, 10, 10, 10, 10, 15,
                            -10, 5, 0, 0, 0, 0, 5, -10,
                            -20, -10, -10, -10, -10, -10, -10, -20 };

            int[] rooks = { -10, 0, 20, 30, 30, 20, 0, -10,
                            5, 20, 10, 10, 10, 10, 20, 5,
                            -5, 0, 0, 0, 0, 0, 0, -5,
                            -5, 0, 0, 0, 0, 0, 0, -5,
                            -5, 0, 0, 0, 0, 0, 0, -5,
                            -5, 0, 0, 0, 0, 0, 0, -5,
                            30, 30, 30, 30, 30, 30, 30, 30,
                            20, 20, 20, 20, 20, 20, 20, 20 };

            int[] king = { 0, 30, 20, -30, -40, 20, 30, 20,
                        -30, -40, -40, -50, -50, -40, -40, -30,
                        -30, -40, -40, -50, -50, -40, -40, -30,
                        -30, -40, -40, -50, -50, -40, -40, -30,
                        -20, -30, -30, -40, -40, -30, -30, -20,
                        -10, -20, -20, -20, -20, -20, -20, -10,
                        0, 0, 0, 0, 0, 0, 0, 0,
                        0, 0, 10, 0, 0, 0, 0, 0 };
                        
            int[] king_end = { -50,-40,-30,-20,-20,-30,-40,-50,
                            -30,-20,-10,  0,  0,-10,-20,-30,
                            -30,-10, 20, 30, 30, 20,-10,-30,
                            -30,-10, 30, 40, 40, 30,-10,-30,
                            -30,-10, 30, 40, 40, 30,-10,-30,
                            -30,-10, 20, 30, 30, 20,-10,-30,
                            -30,-30,  0,  0,  0,  0,-30,-30,
                            -50,-30,-30,-30,-30,-30,-30,-50 };

            int[] pawns = { 0, 0, 0, 0, 0, 0, 0, 0,
                            20, 20, 30, 10, 10, 20, 20, 20,
                            15, 15, 20, 30, 30, 20, 15, 15,
                            15, 0, 10, 50, 50, 10, 0, 15,
                            0, 0, 0, 40, 40, 0, 0, 20,
                            20, -5, -10, 0, 0, -10, -5, 5,
                            5, 10, 10, -20, -20, 10, 10, 5,
                            0, 0, 0, 0, 0, 0, 0, 0 };

            int[] pawns_end = { 0,  0,  0,  0,  0,  0,  0,  0,
                            -10, -10, -10, -10, -10, -10, -10, -10,
                            5, 5, 5, 5, 5,5, 5, 5,
                            10,  10, 10, 10, 10, 10,  10,  10,
                            20,  20,  20, 20, 20,  20,  20,  20,
                            40, 40,40,  40,  40,40, 40,40,
                            70, 70, 70,70,70, 70, 70, 70,
                            0,  0,  0,  0,  0,  0,  0,  0 };

    
    // https://www.chessprogramming.org/Transposition_Table
    struct TTEntry {
        public ulong key;
        public Move move;
        public int depth, score, bound;
        public TTEntry(ulong _key, Move _move, int _depth, int _score, int _bound) {
            key = _key; move = _move; depth = _depth; score = _score; bound = _bound;
        }
    }

    const int entries = (1 << 20);
    TTEntry[] tt = new TTEntry[entries];


    public int Evaluate(Board board) {

            int score=GetPiecePositioning(board);
            
            return board.IsWhiteToMove ? score : -score;
    }

    public int GetPiecePositioning(Board board)
        {
            int score = 0;
            
            ulong allPiecesBitboard = board.AllPiecesBitboard;
            int totalPieces = BitboardHelper.GetNumberOfSetBits(allPiecesBitboard);

            double factor = (32.0 - totalPieces) / 32.0;

            
            foreach(bool stm in new[] {true, false}) {
                for(var p = PieceType.Pawn; p <= PieceType.King; p++) {
                    int piece = (int)p;
                    ulong mask = board.GetPieceBitboard(p, stm);
                    while(mask != 0) {
                        int squareIndex = BitboardHelper.ClearAndGetIndexOfLSB(ref mask);
                        score+=getPieceSquareScore(p,squareIndex,factor)+pieceVal[piece];
                    }
                }
                score*=-1;
            }
            

            return score;
        }
    
    public int getPieceSquareScore(PieceType pieceType,int index,double factor){
        if (pieceType==PieceType.Pawn){
            return (int)(pawns[index] + (pawns_end[index] - pawns[index]) * factor);
        }
        if (pieceType==PieceType.Knight){
            return knights[index];
        } 
        if (pieceType==PieceType.Bishop){
            return bishops[index];
        } 
        if (pieceType==PieceType.Rook){
            return rooks[index];
        } 
        if (pieceType==PieceType.King){
            return (int)(king[index] + (king_end[index] - king[index]) * factor);
        }
        if (pieceType==PieceType.Queen){
            return bishops[index]+rooks[index];
        } 

        Console.WriteLine("NO PIECES ON THE BOARD TO EVALUATE");
        return 0;
    }

    // https://www.chessprogramming.org/Negamax
    // https://www.chessprogramming.org/Quiescence_Search
    public int Search(Board board, Timer timer, int alpha, int beta, int depth, int ply) {
        ulong key = board.ZobristKey;
        bool qsearch = depth <= 0;
        bool notRoot = ply > 0;
        int best = -30000;

        // Check for repetition (this is much more important than material and 50 move rule draws)
        if(notRoot && board.IsRepeatedPosition())
            return 0;

        TTEntry entry = tt[key % entries];

        // TT cutoffs
        if(notRoot && entry.key == key && entry.depth >= depth && (
            entry.bound == 3 // exact score
                || entry.bound == 2 && entry.score >= beta // lower bound, fail high
                || entry.bound == 1 && entry.score <= alpha // upper bound, fail low
        )) return entry.score;

        int eval = Evaluate(board);

        // Quiescence search is in the same function as negamax to save tokens
        if(qsearch) {
            best = eval;
            if(best >= beta) return best;
            alpha = Math.Max(alpha, best);
        }

        // Generate moves, only captures in qsearch
        Move[] moves = board.GetLegalMoves(qsearch);
        int[] scores = new int[moves.Length];

        // Score moves
        for(int i = 0; i < moves.Length; i++) {
            Move move = moves[i];
            // TT move
            if(move == entry.move) scores[i] = 1000000;
            // https://www.chessprogramming.org/MVV-LVA
            else if(move.IsCapture) scores[i] = 100 * (int)move.CapturePieceType - (int)move.MovePieceType;
        }

        Move bestMove = Move.NullMove;
        int origAlpha = alpha;

        // Search moves
        for(int i = 0; i < moves.Length; i++) {
            if(timer.MillisecondsElapsedThisTurn >= timer.MillisecondsRemaining / 30) return 30000;

            // Incrementally sort moves
            for(int j = i + 1; j < moves.Length; j++) {
                if(scores[j] > scores[i])
                    (scores[i], scores[j], moves[i], moves[j]) = (scores[j], scores[i], moves[j], moves[i]);
            }

            Move move = moves[i];
            board.MakeMove(move);
            int score = -Search(board, timer, -beta, -alpha, depth - 1, ply + 1);
            board.UndoMove(move);

            // New best move
            if(score > best) {
                best = score;
                bestMove = move;
                if(ply == 0) bestmoveRoot = move;

                // Improve alpha
                alpha = Math.Max(alpha, score);

                // Fail-high
                if(alpha >= beta) break;

            }
        }

        // (Check/Stale)mate
        if(!qsearch && moves.Length == 0) return board.IsInCheck() ? -30000 + ply : 0;

        // Did we fail high/low or get an exact score?
        int bound = best >= beta ? 2 : best > origAlpha ? 3 : 1;

        // Push to TT
        tt[key % entries] = new TTEntry(key, bestMove, depth, best, bound);

        return best;
    }

    public Move Think(Board board, Timer timer)
    {
        bestmoveRoot = Move.NullMove;
        // https://www.chessprogramming.org/Iterative_Deepening
        for(int depth = 1; depth <= 50; depth++) {
            int score = Search(board, timer, -30000, 30000, depth, 0);
            Console.WriteLine("DEPTH: "+depth);
            if(timer.MillisecondsElapsedThisTurn >= timer.MillisecondsRemaining / 30){
                
                Console.WriteLine("BREAK");
                break;
            }
                
        }
        return bestmoveRoot.IsNull ? board.GetLegalMoves()[0] : bestmoveRoot;
    }
}
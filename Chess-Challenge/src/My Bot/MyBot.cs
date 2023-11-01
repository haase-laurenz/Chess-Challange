﻿﻿using ChessChallenge.API;
using System;
using System.Linq;
using System.Collections.Generic;

public class MyBot : IChessBot
{   
    int maxdepth;
    Move bestmoveRoot = Move.NullMove;

    // https://www.chessprogramming.org/PeSTO%27s_Evaluation_Function
    int[] pieceVal = {0, 100, 310, 330, 500, 1000, 10000 };
    int[] piecePhase = {0, 0, 1, 1, 2, 4, 0};
    ulong[] psts = {657614902731556116, 420894446315227099, 384592972471695068, 312245244820264086, 364876803783607569, 366006824779723922, 366006826859316500, 786039115310605588, 421220596516513823, 366011295806342421, 366006826859316436, 366006896669578452, 162218943720801556, 440575073001255824, 657087419459913430, 402634039558223453, 347425219986941203, 365698755348489557, 311382605788951956, 147850316371514514, 329107007234708689, 402598430990222677, 402611905376114006, 329415149680141460, 257053881053295759, 291134268204721362, 492947507967247313, 367159395376767958, 384021229732455700, 384307098409076181, 402035762391246293, 328847661003244824, 365712019230110867, 366002427738801364, 384307168185238804, 347996828560606484, 329692156834174227, 365439338182165780, 386018218798040211, 456959123538409047, 347157285952386452, 365711880701965780, 365997890021704981, 221896035722130452, 384289231362147538, 384307167128540502, 366006826859320596, 366006826876093716, 366002360093332756, 366006824694793492, 347992428333053139, 457508666683233428, 329723156783776785, 329401687190893908, 366002356855326100, 366288301819245844, 329978030930875600, 420621693221156179, 422042614449657239, 384602117564867863, 419505151144195476, 366274972473194070, 329406075454444949, 275354286769374224, 366855645423297932, 329991151972070674, 311105941360174354, 256772197720318995, 365993560693875923, 258219435335676691, 383730812414424149, 384601907111998612, 401758895947998613, 420612834953622999, 402607438610388375, 329978099633296596, 67159620133902};

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

    int GetPieceValue(Piece piece)
    {
        // Hier kannst du die Wertigkeiten der verschiedenen Figuren anpassen
        var pieceValues = new Dictionary<PieceType, int>
        {
            { PieceType.None, 0 }, // Leeres Feld hat den Wert 0
            { PieceType.Pawn, 100 },
            { PieceType.Knight, 300 },
            { PieceType.Bishop, 320 },
            { PieceType.Rook, 500 },
            { PieceType.Queen, 900 },
            { PieceType.King, 0 }
        };

        return pieceValues[piece.PieceType];
    }
    int GetPieceCount(Board board){

            return board.GetAllPieceLists()
            .SelectMany(pieceList => pieceList)
            .Sum(piece => piece.IsWhite ? GetPieceValue(piece) : -GetPieceValue(piece));
    }

    public int Evaluate(Board board) {

            int score=GetPieceCount(board)+GetPiecePositioning(board);
            
            return board.IsWhiteToMove ? score : -score;
    }

    int GetPiecePositioning(Board board)
        {
            int score = 0;
            
            ulong allPiecesBitboard = board.AllPiecesBitboard;
            int totalPieces = BitboardHelper.GetNumberOfSetBits(allPiecesBitboard);

            double factor = (32.0 - totalPieces) / 32.0;

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

            for (int index=0;index<64;index++)
            {
                
                king[index] = (int)(king[index] + (king_end[index] - king[index]) * factor);
                pawns[index] = (int)(pawns[index] + (pawns_end[index] - pawns[index]) * factor);
                
            }           

            foreach (PieceList pieceList in board.GetAllPieceLists())
            {
                int[] pieceValues;

                switch (pieceList.TypeOfPieceInList)
                {
                    case PieceType.Knight:
                        pieceValues = knights;
                        break;
                    case PieceType.Bishop:
                        pieceValues = bishops;
                        break;
                    case PieceType.Rook:
                        pieceValues = rooks;
                        break;
                    case PieceType.King:
                        pieceValues = king;
                        break;
                    case PieceType.Pawn:
                        pieceValues = pawns;
                        break;
                    default:
                        continue;
                }

                foreach (Piece piece in pieceList)
                {   
                    int mirroredRow = 7 - piece.Square.Rank;
                    int mirroredSquareIndex = mirroredRow * 8 + piece.Square.File;
                    int pieceValue = piece.IsWhite? pieceValues[piece.Square.Index] : pieceValues[mirroredSquareIndex];
                    score += piece.IsWhite ? pieceValue : -pieceValue;
                }
            }

            return score;
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
using ChessChallenge.API;
using System;
using System.Collections.Generic;

public class MoveCache
{
    public Dictionary<string, int>       valueCacheByMove  = new Dictionary<string, int>();
    // neat idea, but what about collisions?
    // public Dictionary<int,    string>    moveCacheByValue  = new Dictionary<int,    string>();
    public Dictionary<string, MoveCache> nestedCache;
    public string[] sortedMoves;
    public int bestMove = int.MinValue;

    public bool TryGetValue(string key, int depth, out int value)
    {
        if (nestedCache.TryGetValue(depth, out var depthCache))
        {
            return depthCache.valueCache.TryGetValue(key, out value);
        }
        value = 0;
        return false;
    }

    public void Add(string key, int depth, int value)
    {
        if (!nestedCache.TryGetValue(depth, out var depthCache))
        {
            depthCache = new MoveCache();
            nestedCache[depth] = depthCache;
        }

        depthCache.cache[key] = value;
    }
}


public class MyBot : IChessBot
{
    // Piece values: null, pawn, knight, bishop, rook, queen, king
    int[] pieceValues = { 0, 100, 250, 300, 500, 900, 10000 };
    int[] moveValues = { 0, 100, 80, 85, 90, 95, 70 };

    public Move Think(Board board, Timer timer)
    {
        Move[] allMoves = board.GetLegalMoves();
       
        // Pick a random move to play if nothing better is found
        Random rng = new();
        Move moveToPlay = allMoves[rng.Next(allMoves.Length)];
        int highestValueMove = 0;

        foreach (Move move in allMoves)
        {
            // Always play checkmate in one
            if (MoveIsCheckmate(board, move))
            {
                return move;
            }
            
            // Only promote to queens
            if ((move.IsPromotion) & !((int)move.PromotionPieceType == 5))
            {  continue; }
            // Evaluate the game state
            int thisMoveValue = moveValue(board, move) + rng.Next(50);
            // Look ahead, and never play a move that will allow checkmate in one
            board.MakeMove(move);
            Move[] opponentMoves = board.GetLegalMoves();
            bool isMate = false;
            int bestResponseValue = 0;
            foreach (Move response in opponentMoves)
            {
                int responseValue = moveValue(board, response);
                if (bestResponseValue < responseValue)
                { bestResponseValue = responseValue; }
                board.MakeMove(response);
                if (board.IsInCheckmate())
                {
                    isMate = true;
                    board.UndoMove(response);
                    break;
                }
                board.UndoMove(response);
            }
            board.UndoMove(move);
            if (isMate) { continue; }
            thisMoveValue -= bestResponseValue;
            // decide on the move
            if ( highestValueMove < thisMoveValue )
            {
                moveToPlay = move;
                highestValueMove = thisMoveValue;
            }
            
        }

        return moveToPlay;
    }

    // Test if this move gives checkmate
    bool MoveIsCheckmate(Board board, Move move)
    {
        board.MakeMove(move);
        bool isMate = board.IsInCheckmate();
        board.UndoMove(move);
        return isMate;
    }

    // Calculate the approximate value of this move
    int moveValue(Board board, Move move)
    {
        int thisMoveValue  = 0;
        Square targetSquare = move.TargetSquare;
        if (board.SquareIsAttackedByOpponent(targetSquare))
        {
            thisMoveValue = -25 - pieceValues[ (int)move.MovePieceType ];
        } else {
            thisMoveValue =       moveValues[ (int)move.MovePieceType ];
        }
        if (move.IsCapture)
        {
            thisMoveValue += pieceValues[(int)move.CapturePieceType];
        }
        if (move.IsPromotion) { thisMoveValue += 800; }
        return thisMoveValue;
    }
}
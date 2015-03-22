using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace TerraFirma
{
    sealed internal class Game
    {

        private Board m_GameBoard;
        private int m_EngineSide = -1;

        //clocks foreach side
        private Clock[] m_Clocks;
        private bool m_toContinue = true;
        private bool m_editMode = false;

        //for fast debugging read input from file.
        private bool m_GetResponseFromFile = false;

        //disable when playing games against yourself , to avoid script.txt lock.
        private bool m_WriteToScript = true;
        private List<string> m_FileResponse;

        public Game()
        {
            m_GameBoard = new Board();
            OpeningBook.ResetBook();

            if (m_GetResponseFromFile == true)
            {
                StreamReader file = new StreamReader("script.txt");
                m_FileResponse = new List<string>();
                while (!file.EndOfStream)
                    m_FileResponse.Add(file.ReadLine());
            }

            m_GameBoard.StartingPosition();

        }

        public void BeginGame()
        {
            WinBoardGame();
        }

        public void WinBoardGame()
        {
            m_Clocks = new Clock[2];
            m_Clocks[Side.White] = new Clock();
            m_Clocks[Side.Black] = new Clock();

            AIPlayer.Moved += new MoveEventHandler(m_EnginePlayer_Moved);
            //m_GameBoard.CurrentPlayer = (Side)(((int)currentPlayer + 1) & 0x1);
            //move = Players[currentPlayer].Move();
            Global.WriteToLog("-----------------" + DateTime.Now.ToString() + "-----------------");
            while (m_toContinue)
            {
                string input;
                if (m_GetResponseFromFile)
                {
                    while (AIPlayer.IsThinking)
                        System.Threading.Thread.Sleep(2000);

                    input = m_FileResponse[0];
                    m_FileResponse.RemoveAt(0);

                    //pause at level
                    if (m_FileResponse.Count == 2)
                    {
                        int x = 1;
                        if (x == 1)
                            x = 2;
                    }
                }
                else
                {
                    while (AIPlayer.IsThinking)
                        System.Threading.Thread.Sleep(2000);

                    input = Console.ReadLine();

                    if (m_WriteToScript)
                        Global.WriteToScript(input);
                }

                Global.WriteToLog("I:" + input);
                string arg = String.Empty;
                string commandType = String.Empty;
                int pos = input.IndexOf(' ');
                if (pos > 0)
                {
                    commandType = input.Substring(0, pos);
                    if (input.Length > pos + 1)
                        arg = input.Substring(pos + 1);
                }
                else
                    commandType = input;


                WinBoardCommands command = WinBoardCommands.Nopost;
                string response = String.Empty;

                if (m_editMode)
                {
                    /*if (commandType.Length > 0)
                    {
                        if (commandType.Length > 1)
                        {
                            int Col = input[1] - 'a';
                            int Row = int.Parse(input[2].ToString()) - 1;
                            int position = Row * 8 + Col;
                            switch (commandType[0])
                            {
                                case 'P':
                                    m_GameBoard.AddPiece(position, Piece.BlackPawn + m_GameBoard.CurrentPlayer);
                                    break;
                                case 'R':
                                    m_GameBoard.AddPiece(position, Piece.BlackRook + m_GameBoard.CurrentPlayer);
                                    break;
                                case 'Q':
                                    m_GameBoard.AddPiece(position, Piece.BlackQueen + m_GameBoard.CurrentPlayer);
                                    break;
                                case 'K':
                                    m_GameBoard.AddPiece(position, Piece.BlackKing + m_GameBoard.CurrentPlayer);
                                    break;
                                case 'B':
                                    m_GameBoard.AddPiece(position, Piece.BlackBishop + m_GameBoard.CurrentPlayer);
                                    break;
                                case 'N':
                                    m_GameBoard.AddPiece(position, Piece.BlackKnight + m_GameBoard.CurrentPlayer);
                                    break;
                            }
                        }
                        else
                        {
                            switch (commandType[0])
                            {
                                case '#':
                                    int currentPlayer = m_GameBoard.CurrentPlayer;
                                    m_GameBoard = new Board();
                                    m_GameBoard.ClearedBoard();
                                    m_GameBoard.CurrentPlayer = currentPlayer;
                                    break;
                                case '.':
                                    m_editMode = false;
                                    break;
                            }
                        }
                    }*/
                }
                else
                {
                    try
                    {
                        command = (WinBoardCommands)Enum.Parse(typeof(WinBoardCommands), commandType, true);
                    }
                    catch (Exception ex)
                    {
                        switch (commandType)
                        {
                            case "?": command = WinBoardCommands.QMark;
                                break;
                            default:
                                command = WinBoardCommands.Unknown;
                                break;
                        }
                    }


                    switch (command)
                    {
                        case WinBoardCommands.New:
                            break;
                        case WinBoardCommands.Protover:
                            response = "feature time=0 colors=0 usermove=1 done=1";
                            break;
                        case WinBoardCommands.Quit:
                            m_toContinue = false;
                            AIPlayer.Dispose();
                            break;
                        case WinBoardCommands.Force:
                            AIPlayer.ToForce = true;
                            break;
                        case WinBoardCommands.Level:
                            string[] timeData;
                            timeData = arg.Split(' ');
                            if (timeData[1].Contains(':'))
                            {
                                string[] timeDataParts = timeData[1].Split(':');
                                int minutes = int.Parse(timeDataParts[0]), seconds = int.Parse(timeDataParts[1]);
                                m_Clocks[Side.Black].Time = m_Clocks[Side.White].Time = TimeSpan.FromMinutes(minutes).Add(TimeSpan.FromSeconds(seconds));
                            }
                            else
                            {
                                int minutes = int.Parse(timeData[1]);
                                m_Clocks[Side.Black].Time = m_Clocks[Side.White].Time = TimeSpan.FromMinutes(minutes);
                            }
                            m_Clocks[Side.Black].Incremental = m_Clocks[Side.White].Incremental = TimeSpan.FromSeconds(int.Parse(timeData[2]));
                            break;
                        case WinBoardCommands.Sd:
                            int depth;
                            if (int.TryParse(arg, out depth))
                            {
                                AIPlayer.Depth = depth;
                            }
                            else
                            {
                                //out error
                            }
                            break;
                        case WinBoardCommands.QMark:
                            AIPlayer.Move(m_GameBoard);
                            break;
                        case WinBoardCommands.Usermove:
                            if (m_EngineSide == -1)
                            {
                                m_EngineSide = Side.Black;
                                AIPlayer.Clock = m_Clocks[m_EngineSide];
                            }
                            AIPlayer.StopPondering = true;

                            var moves = MovesGenerator.GetMoves(m_GameBoard, false);
                            int sourceCol = arg[0] - 'a';
                            int sourceRow = int.Parse(arg[1].ToString()) - 1;
                            int destCol = arg[2] - 'a';
                            int destRow = int.Parse(arg[3].ToString()) - 1;
                            int source = sourceRow * 8 + sourceCol;
                            int dest = destRow * 8 + destCol;
                            Move move = (from anyMove in moves
                                         where anyMove.SourceSquare == source && anyMove.DestinationSquare == dest
                                         select anyMove).ElementAtOrDefault(0);
                            if (move == null)
                            {
                                response = "Illegal move: " + arg;
                            }
                            else
                            {
                                m_GameBoard.ApplyMove(move);
                                OpeningBook.MoveApplied(move);
                                m_Clocks[1 - m_GameBoard.CurrentPlayer].Stop();
                                m_Clocks[m_GameBoard.CurrentPlayer].Start();
                                AIPlayer.Move(m_GameBoard);
                            }
                            break;
                        case WinBoardCommands.Go:
                            if (m_EngineSide == -1)
                            {
                                m_EngineSide = Side.White;
                                AIPlayer.Clock = m_Clocks[m_EngineSide];
                            }

                            if (m_EngineSide != m_GameBoard.CurrentPlayer)
                                break;

                            AIPlayer.ToForce = false;
                            m_EngineSide = m_GameBoard.CurrentPlayer;
                            AIPlayer.Clock = m_Clocks[m_EngineSide];
                            m_Clocks[m_EngineSide].Start();
                            AIPlayer.Move(m_GameBoard);
                            break;
                        case WinBoardCommands.Accepted:
                            break;
                        case WinBoardCommands.Result:
                            //announce defeat to winboard ?
                            break;
                        case WinBoardCommands.Edit:
                            m_editMode = true;
                            break;
                        default:
                            response = "Error (unknown command): " + commandType;
                            break;
                    }

                    if (response != string.Empty)
                    {
                        Global.WriteToLog("O:" + response);
                        Console.WriteLine(response);
                    }
                }
            }
        }

        void m_EnginePlayer_Moved(Move move)
        {
            if (move == null)
            {
                Console.WriteLine("resign");
            }
            else
            {
                m_GameBoard.ApplyMove(move);
                string response = "move " + move.ToString();
                Global.WriteToLog("O: " + response);
                Console.WriteLine(response);

                AIPlayer.Ponder(Board.Clone(m_GameBoard));
            }

        }


    }
}

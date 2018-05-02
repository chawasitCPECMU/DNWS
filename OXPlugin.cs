using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace DNWS
{
    public class Player
    {
        protected String _name;
        public String Name
        {
            get { return _name; }
        }
        protected String _password;
        public string Password
        {
            get { return _password; }
        }
        protected int _winNum = 0;
        public int WinNum
        {
            get { return _winNum; }
        }
        protected int _drawNum = 0;
        public int DrawNum
        {
            get { return _drawNum; }
        }
        protected int _lossNum = 0;
        public int LostNum
        {
            get { return _lossNum; }
        }

        public Player(String name, String password)
        {
            _name = name;
            _password = password;
        }

        public Boolean Login(String name, String password)
        {
            if (name.Equals(_name) && password.Equals(_password))
            {
                return true;
            }
            return false;
        }

        public void Win()
        {
            _winNum++;
        }
        public void Draw()
        {
            _drawNum++;
        }
        public void Loss()
        {
            _lossNum++;
        }
    }

    public class OXBoard
    {
        protected char[,] _board;
        public char[,] Board
        {
            get { return _board; }
        }
        public static char X_PLAYER = 'X';
        public static char O_PLAYER = 'O';
        public static char DRAW = 'D';
        public static char EMPTY = ' ';
        protected int _playCount = 0;

        public OXBoard()
        {
            _board = new char[3, 3];
            for (int i = 0; i != 3; i++)
            {
                for (int j = 0; j != 3; j++)
                {
                    _board[i, j] = EMPTY;
                }
            }
        }

        public Boolean Place(int row, int col, char player)
        {
            if (player != X_PLAYER && player != O_PLAYER)
            {
                return false;
            }
            if (_board[row, col] != EMPTY)
            {
                return false;
            }
            _board[row, col] = player;
            _playCount++;
            return true;
        }

        public char CheckWin()
        {
            for (int i = 0; i != 3; i++)
            {
                if (_board[i, 0] != EMPTY && _board[i, 0] == _board[i, 1] && _board[i, 1] == _board[i, 2])
                {
                    return _board[i, 0];
                }
                if (_board[0, i] != EMPTY && _board[0, i] == _board[1, i] && _board[1, i] == _board[2, i])
                {
                    return _board[0, i];
                }
                if (_board[0, 0] != EMPTY && _board[0, 0] == _board[1, 1] && _board[1, 1] == _board[2, 2])
                {
                    return _board[0, 0];
                }
                if (_board[0, 2] != EMPTY && _board[0, 2] == _board[1, 1] && _board[1, 1] == _board[2, 0])
                {
                    return _board[0, 2];
                }
            }
            if (_playCount == 9)
            {
                return DRAW;
            }
            return EMPTY;
        }

    }

    public class Game
    {
        protected OXBoard _board;
        protected Player _xPlayer;
        protected Player _oPlayer;
        protected char _player = OXBoard.X_PLAYER; // always X first, life is not fair.
        protected int _index;
        protected char _status;

        public OXBoard Board
        {
            get { return _board; }
        }
        public char Status
        {
            set { _status = value; }
            get { return _status; }
        }
        public int Index
        {
            set { _index = value; }
            get { return _index; }
        }

        public char Player
        {
            get { return _player; }
        }

        public Player XPlayer
        {
            get { return _xPlayer; }
            set { _xPlayer = value; }
        }

        public Player OPlayer
        {
            get { return _oPlayer; }
            set { _oPlayer = value; }
        }

        public static char X_WIN = 'x';
        public static char O_WIN = 'O';
        public static char DRAW = 'D';
        public static char CONT = 'C';
        public static char ERROR = 'E';

        public static char CREATED_X = 'X';
        public static char CREATED_O = 'O';

        public Game(Player xPlayer, Player oPlayer)
        {
            _xPlayer = xPlayer;
            _oPlayer = oPlayer;
            _board = new OXBoard();
        }

        public char Turn(int row, int col)
        {
            char win;
            if (_board.Place(row, col, _player))
            {
                if ((win = _board.CheckWin()) != OXBoard.EMPTY)
                {
                    if (win == OXBoard.X_PLAYER)
                    {
                        _xPlayer.Win();
                        _oPlayer.Loss();
                        _status = X_WIN;
                        return X_WIN;
                    }
                    else if (win == OXBoard.O_PLAYER)
                    {
                        _oPlayer.Win();
                        _xPlayer.Loss();
                        _status = O_WIN;
                        return O_WIN;
                    }
                    else if (win == OXBoard.DRAW)
                    {
                        _oPlayer.Draw();
                        _xPlayer.Draw();
                        _status = DRAW;
                        return DRAW;
                    }
                }
                _player = (_player == OXBoard.X_PLAYER) ? OXBoard.O_PLAYER : OXBoard.X_PLAYER;
                _status = CONT;
                return CONT;
            }
            return ERROR;
        }
    }

    // For simplicity, this class will be wrapper
    public class OXPlugin : IPlugin
    {
        private OXGame game;
        public OXPlugin()
        {
            game = OXGame.GetInstance();
        }
        public HTTPResponse GetResponse(HTTPRequest request)
        {
            return game.GetResponse(request);
        }

        public HTTPResponse PostProcessing(HTTPResponse response)
        {
            throw new NotImplementedException();
        }

        public void PreProcessing(HTTPRequest request)
        {
            throw new NotImplementedException();
        }
    }
    public class OXGame
    {
        private static OXGame _instance = null;
        protected List<Player> _playerList;
        protected List<Game> _gameList;

        private OXGame()
        {
            _playerList = new List<Player>();
            _gameList = new List<Game>();
            AddPlayer("pruet", "pruet");
            AddPlayer("lulu", "lulu");
        }

        //Singleton, so people can play game together
        public static OXGame GetInstance()
        {
            if (_instance == null)
            {
                _instance = new OXGame();
            }
            return _instance;
        }
        public HTTPResponse GetResponse(HTTPRequest request)
        {
            HTTPResponse response = new HTTPResponse(200);
            StringBuilder sb = new StringBuilder();
            Dictionary<String, String> parameters = new Dictionary<string, string>();
            sb.Append("<h1>OX Game</h1>");
            String[] parts = Regex.Split(request.Filename, "[?]");
            if (parts.Length > 1)
            {
                // http://stackoverflow.com/a/4982122
                parameters = parts[1].Split('&').Select(x => x.Split('=')).ToDictionary(x => x[0].ToLower(), x => x[1]);
            }

            if (!parameters.ContainsKey("action")) // No action? go to Homepage
            {

                // show login screen
                if (!parameters.ContainsKey("username"))
                {
                    sb.Append("<h2>Login</h2>");
                    sb.Append("<form method=\"get\">");
                    sb.Append("Username: <input type=\"text\" name=\"username\" value=\"\" /> <br />");
                    sb.Append("Password: <input type=\"password\" name=\"password\" value=\"\" /> <br />");
                    sb.Append("<input type=\"submit\" name=\"action\" value=\"Login\" /> <br />");
                    sb.Append("</form>");
                }
                else
                {
                    sb.Append(String.Format("<h2>Wellcome {0}</h2>", parameters["username"]));
                }

                // Show player list
                sb.Append("<h2>Player's List</h2>");
                sb.Append("<table border=\"1\"><tr><td>Name</td><td>Win</td><td>Loss</td><td>Draw</td></tr>");
                foreach (Player player in _playerList)
                {
                    sb.Append(String.Format("<tr><td>{0}</td><td>{1}</td><td>{2}</td><td>{3}</td></tr>", player.Name, player.WinNum, player.LostNum, player.DrawNum));
                }
                sb.Append("</table>");
                sb.Append("<a href=\"/ox?action=newplayer\">Create new player</a>");

                // Show game list
                sb.Append("<h2>Game's List</h2>");
                sb.Append("<table border=\"1\"><tr><td>Game #</td><td>X</td><td>O</td><td>Action</td></tr>");
                foreach (Game game in _gameList)
                {
                    String xPlayer = (game.XPlayer != null) ? game.XPlayer.Name : "--";
                    String oPlayer = (game.OPlayer != null) ? game.OPlayer.Name : "--";
                    sb.Append(String.Format("<tr><td>{0}</td><td>{1}</td><td>{2}</td>", game.Index, xPlayer, oPlayer));

                    if (parameters.ContainsKey("username") && (game.Status == Game.CONT || game.Status == Game.CREATED_O || game.Status == Game.CREATED_X) )
                    {
                        if (parameters["username"] == xPlayer || parameters["username"] == oPlayer)
                        {
                            if (game.Status == Game.CREATED_O || game.Status == Game.CREATED_X)
                            {
                                sb.Append("<td>Wait</td>");
                            }
                            else
                            {
                                sb.Append(String.Format("<td><a href=\"/ox?action=playgame&game={0}&username={1}\">Play</a>", game.Index, parameters["username"]));
                            }
                        }
                        else
                        {
                            sb.Append(String.Format("<td><a href=\"/ox?action=joingame&game={0}&username={1}\">Join</a>", game.Index, parameters["username"]));
                        }
                    }
                    else
                    {
                        if (game.Status == Game.X_WIN)
                        {
                            sb.Append("<td>X Win</td>");
                        }
                        else if (game.Status == Game.O_WIN)
                        {
                            sb.Append("<td>O Win</td>");
                        }
                        else if (game.Status == Game.DRAW)
                        {
                            sb.Append("<td>Draw</td>");
                        } 
                        
                        else if(game.Status != Game.CONT)
                        {
                            sb.Append("<td>WAITING</td>");
                        }
                    }

                }
                sb.Append("</tr></table>");
                if (parameters.ContainsKey("username"))
                {
                    sb.Append(String.Format("<a href=\"/ox?action=startgame&username={0}\">Start new game</a><br />", parameters["username"]));

                }

            }
            else //Action page
            {
                if (parameters["action"] == "newplayer") // create new player page
                {
                    sb.Append("<h2>Create new player</h2>");
                    sb.Append("<form method=\"get\">");
                    sb.Append("Username: <input type=\"text\" name=\"username\" value=\"\" /> <br />");
                    sb.Append("Password: <input type=\"text\" name=\"password\" value=\"\" /> <br />");
                    sb.Append("<input type=\"hidden\" name=\"action\" value=\"addnewplayer\" /> <br />");
                    sb.Append("<input type=\"submit\" name=\"submit\" value=\"Login\" /> <br />");
                    sb.Append("</form>");
                }
                else if (parameters["action"] == "addnewplayer") // create new player logic
                {
                    if (parameters.ContainsKey("username") && parameters["username"] != "" && parameters.ContainsKey("password") && parameters["password"] != "")
                    {
                        AddPlayer(parameters["username"].Trim(), parameters["password"].Trim());
                        sb.Append("<h2>Player added successfully</h2>");
                        sb.Append(String.Format("Please note your login is <b>{0}</b> and password is <b>{1}</b>. <br />", parameters["username"], parameters["password"]));
                        sb.Append(String.Format("<a href=\"/ox?username={0}\">Click here to go back to home page</a>", parameters["username"]));
                    }
                    else
                    {
                        sb.Append("<h2>Error: Username/password are missing</h2>");
                        sb.Append("<a href=\"/ox\">Click here to go back to home page</a>");

                    }
                }
                else if (parameters["action"] == "Login")
                {
                    Boolean succ = false;
                    if (parameters.ContainsKey("username") && parameters["username"] != "" && parameters.ContainsKey("password") && parameters["password"] != "")
                    {
                        foreach (Player player in _playerList)
                        {
                            if (succ = player.Login(parameters["username"], parameters["password"]))
                            {
                                break;
                            }
                        }
                        if (succ)
                        {
                            sb.Append("<h2>Login successfully</h2>");
                            sb.Append(String.Format("<a href=\"/ox?action=startgame&username={0}\">Start new game</a><br /><br />", parameters["username"]));
                            sb.Append(String.Format("<a href=\"/ox?username={0}\">Click here to go back to home page.</a>", parameters["username"]));
                        }
                        else
                        {
                            sb.Append("<h2>Error: Incorrect Username/password</h2>");
                            sb.Append("<a href=\"/ox\">Click here to go back to home page</a>");
                        }
                    }
                    else
                    {
                        sb.Append("<h2>Error: Username/password are missing</h2>");
                        sb.Append("<a href=\"/ox\">Click here to go back to home page</a>");
                    }
                }
                else if (parameters["action"] == "startgame") // create new game
                {
                    sb.Append("<h2>Start new game</h2>");
                    sb.Append(String.Format("Choose side: <a href=\"/ox?action=chooseside&side=x&username={0}\">X</a> or <a href=\"/ox?action=chooseside&side=x&username={0}\">O</a>?<br /><br />", parameters["username"]));
                    sb.Append(String.Format("<a href=\"/ox?username={0}\">Click here to go back to home page.</a>", parameters["username"]));
                }
                else if (parameters["action"] == "chooseside")
                {
                    int id;
                    Player player = GetPlayerByUserName(parameters["username"]);
                    if (player == null)
                    {
                        sb.Append(String.Format("<h2>Error, user {0} not found</h2>", parameters["username"]));
                        sb.Append("<a href=\"/ox\">Click here to go back to home page (you will need to login again)</a>");
                    }
                    else
                    {
                        if (parameters["side"] == "x") // choose to play as X
                        {
                            id = NewGame(player, null);
                            Game game = GetGameByID(id);
                            game.Status = Game.CREATED_X;
                        }
                        else
                        {
                            id = NewGame(player, null);
                            Game game = GetGameByID(id);
                            game.Status = Game.CREATED_O;
                        }
                        sb.Append(String.Format("<h2>Start new game by {0} as {1}. The game ID is {2}.</h2>", parameters["username"], parameters["side"], id));
                        sb.Append("You will need to wait for another player to join the game.<br /><br />");
                        sb.Append(String.Format("<a href=\"/ox?username={0}\">Click here to go back to home page.</a>", parameters["username"]));

                    }

                }
                else if (parameters["action"] == "joingame")
                {
                    int id = Int16.Parse(parameters["game"]);
                    Game game = GetGameByID(id);
                    if (game.Status == Game.CREATED_X || game.Status == Game.CREATED_O)
                    {

                        if (game.Status == Game.CREATED_X)
                        {
                            game.OPlayer = GetPlayerByUserName(parameters["username"]);
                        }
                        else
                        {
                            game.XPlayer = GetPlayerByUserName(parameters["username"]);
                        }
                        game.Status = Game.CONT;

                        sb.Append("<h2>Game is started</h2>");
                        sb.Append(String.Format("<a href=\"/ox?action=playgame&username={0}&game={1}\">Click here to go to your game</a><br /><br />", parameters["username"], id));
                        sb.Append(String.Format("<a href=\"/ox?username={0}\">Click here to go back to home page.</a>", parameters["username"]));
                    }
                    else
                    {
                        sb.Append("<h2>Error: can't join the game</h2> Game is either finished or someone already joins the game. Please go back to home page to join the other games or start a new one.");
                        sb.Append(String.Format("<a href=\"/ox?username={0}\">Click here to go back to home page.</a>", parameters["username"]));

                    }
                }
                else if (parameters["action"] == "playgame")
                {
                    Game game = GetGameByID(Int16.Parse(parameters["game"]));
                    char myPlayer;
                    char gameStatus = Game.CONT;
                    
                 
                    if (game == null)
                    {
                        sb.Append("<h2>Error: game doesn't exists, start one at home page.");
                        sb.Append(String.Format("<a href=\"/ox?username={0}\">Click here to go back to home page.</a>", parameters["username"]));
                    }
                    else
                    {
                        if (parameters["username"] == game.XPlayer.Name)
                        {
                            sb.Append(String.Format("<h2>Game {0} between X:<u>{1}</u> and O:{2}</h2>", parameters["game"], game.XPlayer.Name, game.OPlayer.Name));
                        }
                        else
                        {
                            sb.Append(String.Format("<h2>Game {0} between X:{1} and O:<u>{2}</u></h2>", parameters["game"], game.XPlayer.Name, game.OPlayer.Name));
                        }
                        if(game.XPlayer.Name == parameters["username"]) {
                            myPlayer = OXBoard.X_PLAYER;
                        } else {
                            myPlayer = OXBoard.O_PLAYER;
                        }
                        if(parameters.ContainsKey("row") && parameters.ContainsKey("col")) // User wants to play
                        {
                            gameStatus = game.Turn(Int16.Parse(parameters["row"]), Int16.Parse(parameters["col"]));
                        }

                        if (game.Status == Game.X_WIN)
                        {
                            sb.Append("<h2>X WIN!!!</h2>");
                        }
                        else if (game.Status == Game.O_WIN)
                        {
                            sb.Append("<h2>O WIN!!!</h2>");
                        }
                        else if (game.Status == Game.DRAW)
                        {
                            sb.Append("<h2>Draw</h2>");
                        }
                        else // Game is not finished yet
                        {
                            char[,] board = game.Board.Board;
                            char currentPlayer = game.Player;
                            String playLink;

                            if (currentPlayer == myPlayer)
                            {
                                playLink = String.Format("/ox?action=playgame&game={0}&username={1}&side={2}", parameters["game"], parameters["username"], currentPlayer);
                            }
                            else
                            {
                                playLink = "";
                                sb.Append("It's not your turn, please wait for your turn. You might need to reload the page.");
                            }

                            sb.Append("<table border=\"1\">");
                            for (int row = 0; row != 3; row++)
                            {
                                sb.Append("<tr>");
                                for (int col = 0; col != 3; col++)
                                {
                                    sb.Append("<td>");
                                    if (board[row, col] == OXBoard.X_PLAYER)
                                    {
                                        sb.Append(OXBoard.X_PLAYER);
                                    }
                                    else if (board[row, col] == OXBoard.O_PLAYER)
                                    {
                                        sb.Append(OXBoard.O_PLAYER);
                                    }
                                    else
                                    {
                                        if (playLink != "")
                                        {
                                            sb.Append(String.Format("<a href=\"{0}&row={1}&col={2}\">?</a>", playLink, row, col));
                                        }
                                        else
                                        {
                                            sb.Append("&nbsp;");
                                        }
                                    }
                                    sb.Append("</td>");
                                }
                                sb.Append("</tr>");
                            }
                            sb.Append("</table>");
                        }
                        sb.Append(String.Format("<a href=\"/ox?username={0}\">Click here to go back to home page.</a>", parameters["username"]));
                    }                   
                }
            }
            response.Body = Encoding.UTF8.GetBytes(sb.ToString());
            return response;
        }

        public int NewGame(Player XPlayer, Player YPlayer)
        {
            Game game = new Game(XPlayer, YPlayer);
            _gameList.Add(game);
            int index = _gameList.IndexOf(game);
            game.Index = index;
            return index;
        }

        public int AddPlayer(String name, String password)
        {
            Player player = new Player(name, password);
            _playerList.Add(player);
            int index = _playerList.IndexOf(player);
            return index;
        }

        public Game GetGameByID(int index)
        {
            try
            {
                return (Game)_gameList[index];
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return null;
            }
        }

        public Player GetPlayerByUserName(String name)
        {
            try
            {
                foreach (Player player in _playerList)
                {
                    if (player.Name == name) return player;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            return null;

        }
        public Player GetPlayerByID(int index)
        {
            try
            {
                return (Player)_playerList[index];
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return null;
            }
        }
    }
}

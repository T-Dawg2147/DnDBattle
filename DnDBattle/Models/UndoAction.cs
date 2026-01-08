using DnDBattle.Controls;
using DnDBattle.Services;
using DnDBattle.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace DnDBattle.Models
{
    public class TokenAddAction : IUndoabaleAction
    {
        private readonly MainViewModel _vm;
        private readonly Token _token;
        public string Description => $"Add Token {_token?.Name}";

        public TokenAddAction(MainViewModel vm, Token token)
        {
            _vm = vm;
            _token = token;
        }

        public void Do()
        {
            if (!_vm.Tokens.Contains(_token))
                _vm.Tokens.Add(_token);
        }

        public void Undo()
        {
            if (_vm.Tokens.Contains(_token))
                _vm.Tokens.Remove(_token);
        }
    }

    public class TokenRemoveAction : IUndoabaleAction
    {
        private readonly MainViewModel _vm;
        private readonly Token _token;
        public string Description => "Move Token";

        public TokenRemoveAction(MainViewModel vm, Token token)
        {
            _vm = vm; _token = token;
        }

        public void Do()
        {
            if (_vm.Tokens.Contains(_token)) _vm.Tokens.Remove(_token);
        }

        public void Undo()
        {
            if (_vm.Tokens.Contains(_token)) _vm.Tokens.Add(_token);
        }
    }

    public class TokenMoveAction : IUndoabaleAction
    {
        private readonly MainViewModel _vm;
        private readonly Guid _tokenId;
        private readonly int _oldX, _oldY, _newX, _newY;
        public string Description => "Move Token";

        public TokenMoveAction(MainViewModel vm, Token token, int oldX, int oldY, int newX, int newY)
        {
            _vm = vm; _tokenId = token.Id; _oldX = oldX; _oldY = oldY; _newX = newX; _newY = newY;
        }

        private Token FindToken() => _vm.Tokens.FirstOrDefault(t => t.Id == _tokenId);

        public void Do()
        {
            var t = FindToken();
            if (t != null) { t.GridX = _newX; t.GridY = _newY; }
        }

        public void Undo()
        {
            var t = FindToken();
            if (t != null) { t.GridX = _oldX; t.GridY = _oldY; }
        }
    }

    public class PropertyChangedAction<T> : IUndoabaleAction
    {
        private readonly Token _token;
        private readonly string _propName;
        private readonly T _oldValue;
        private readonly T _newValue;
        public string Description => $"Change {_propName}";

        public PropertyChangedAction(Token token, string propName, T oldValue, T newValue)
        {
            _token = token; _propName = propName; _oldValue = oldValue; _newValue = newValue;
        }

        public void Do() => SetValue(_newValue);
        public void Undo() => SetValue(_oldValue);

        private void SetValue(T v)
        {
            var p = _token.GetType().GetProperty(_propName);
            if (p != null && p.CanWrite) p.SetValue(_token, v);
        }
    }

    public class ObstacleAddAction : IUndoabaleAction
    {
        private readonly Obstacle _obs;
        private readonly BattleGridControl _grid;
        public string Description => $"Add Obstacle {_obs?.Label}";

        public ObstacleAddAction(Obstacle obs, BattleGridControl grid)
        {
            _obs = obs; _grid = grid;
        }

        public void Do() => _grid.AddObstacle(_obs);
        public void Undo() => _grid.RemoveObstaclePublic(_obs);
    }

    public class ObstacleRemoveAction : IUndoabaleAction
    {
        private readonly Obstacle _obs;
        private readonly BattleGridControl _grid;
        public string Description => $"Remove Obstacle {_obs?.Label}";

        public ObstacleRemoveAction(Obstacle obs, BattleGridControl grid)
        {
            _obs = obs; _grid = grid;
        }

        public void Do() => _grid.RemoveObstaclePublic(_obs);
        public void Undo() => _grid.AddObstacle(_obs);
    }

    public class ObstacleMoveVertexAction : IUndoabaleAction
    {
        private readonly Obstacle _obs;
        private readonly int _index;
        private readonly Point _oldPos, _newPos;
        private readonly BattleGridControl _grid;
        public string Description => $"Move Vertex {_index}";

        public ObstacleMoveVertexAction(Obstacle obs, int index, Point oldPos, Point newPos, BattleGridControl grid)
        {
            _obs = obs; _index = index; _oldPos = oldPos; _newPos = newPos; _grid = grid;
        }

        public void Do() => _grid.MoveVertexPublic(_obs, _index, _newPos);
        public void Undo() => _grid.MoveVertexPublic(_obs, _index, _oldPos);
    }

    public class LigthAddAction : IUndoabaleAction
    {
        private readonly LightSource _light;
        private readonly BattleGridControl _grid;
        public string Description => $"Add Light";

        public LigthAddAction(LightSource light, BattleGridControl grid)
        {
            _light = light; _grid = grid;
        }
        public void Do() => _grid.AddLight(_light);
        public void Undo() => _grid.RemoveLightPublic(_light);
    }

    public class LigthRemoveAction : IUndoabaleAction
    {
        private readonly LightSource _light;
        private readonly BattleGridControl _grid;
        public string Description => $"Remove Light";

        public LigthRemoveAction(LightSource light, BattleGridControl grid)
        {
            _light = light; _grid = grid;
        }
        public void Do() => _grid.RemoveLightPublic(_light);
        public void Undo() => _grid.AddLight(_light);
    }
    #endregion

    #region Batch actions

    public class BatchTagAction : IUndoabaleAction
    {
        private readonly List<Token> _tokens;
        private readonly string _tag;
        private readonly bool _isAdd;

        public string Description => $"{(_isAdd ? "Add" : "Remove")} tag '{_tag}' from {_tokens.Count} creatures.";

        public BatchTagAction(List<Token> tokens, string tag, bool isAdd)
        {
            _tokens = tokens;
            _tag = tag;
            _isAdd = isAdd;
        }

        public void Do()
        {
            foreach (var t in _tokens)
            {
                if (_isAdd && !t.Tags.Contains(_tag)) t.Tags.Add(_tag);
                else if (!_isAdd) t.Tags.Remove(_tag);
            }
        }

        public void Undo()
        {
            foreach (var t in _tokens)
            {
                if (_isAdd) t.Tags.Contains(_tag);
                else if (!t.Tags.Contains(_tag)) t.Tags.Add(_tag);
            }
        }

    }

    public class BatchTokenAddAction : IUndoabaleAction
    {
        private readonly MainViewModel _vm;
        private readonly List<Token> _tokens;

        public string Description => $"Add {_tokens.Count} Tokens";

        public BatchTokenAddAction(MainViewModel vm, List<Token> tokens)
        {
            _vm = vm;
            _tokens = tokens;
        }

        public void Do()
        {
            foreach (var token in _tokens)
            {
                if (!_vm.Tokens.Contains(token))
                    _vm.Tokens.Add(token);
            }
        }

        public void Undo()
        {
            foreach (var token in _tokens)
            {
                if (_vm.Tokens.Contains(token))
                    _vm.Tokens.Remove(token);
            }
        }
    }

    public class BatchRemoveAction : IUndoabaleAction
    {
        private readonly MainViewModel _vm;
        private readonly List<Token> _tokens;

        public string Description => $"Remove {_tokens.Count} Tokens";

        public BatchRemoveAction(MainViewModel vm, List<Token> tokens)
        {
            _vm = vm;
            _tokens = new List<Token>();
        }

        public void Do()
        {
            foreach (var token in _tokens)
            {
                if (_vm.Tokens.Contains(token))
                    _vm.Tokens.Remove(token);
            }
        }

        public void Undo()
        {
            foreach (var token in _tokens)
            {
                if (!_vm.Tokens.Contains(token))
                    _vm.Tokens.Add(token);
            }
        }
    }
    #endregion
}

using System;
using System.Collections.Generic;
public class UIScreenNavigator
{
    private readonly Stack<UIScreen> _screenStack = new ();
    private readonly Dictionary<Type , UIScreen> _screenMap  = new ();

    public void Register(UIScreen screen)
    {
        _screenMap[screen.GetType()] = screen;
        screen.gameObject.SetActive(false);
    }

    public void Push<T>() where T : UIScreen
    {
        if(_screenMap.TryGetValue(typeof(T) , out UIScreen next) == false) return;

        if (_screenStack.Count > 0)
        {
            _screenStack.Peek().OnPause();
        }

        next.OnEnter();
        _screenStack.Push(next);
    }

    public void Pop()
    {
        if (_screenStack.Count == 0) return;

        UIScreen currrent = _screenStack.Pop();
        currrent.OnExit();

        if (_screenStack.Count > 0)
        {
            _screenStack.Peek().OnResume();
        }
    }

    public void Clear()
    {
        while (_screenStack.Count > 0)
        {
            UIScreen screen = _screenStack.Pop();
            screen.OnExit();
        }
    }
}

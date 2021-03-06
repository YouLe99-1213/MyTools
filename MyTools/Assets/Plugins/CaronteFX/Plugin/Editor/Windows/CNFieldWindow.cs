// ***********************************************************
//	Copyright 2016 Next Limit Technologies, http://www.nextlimit.com
//	All rights reserved.
//
//	THIS SOFTWARE IS PROVIDED 'AS IS' AND WITHOUT ANY EXPRESS OR
//	IMPLIED WARRANTIES, INCLUDING, WITHOUT LIMITATION, THE IMPLIED
//	WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE.
//
// ***********************************************************

using UnityEngine;
using UnityEditor;
using System.Collections;

namespace CaronteFX
{
  public abstract class CNFieldWindow : CarWindow<CNFieldWindow>
  { 
    public enum Type
    {
      normal,
      extended
    }
    //-----------------------------------------------------------------------------------
    protected CNFieldView       View       { get; set; }
    protected CNFieldController Controller { get; set; }
    //-----------------------------------------------------------------------------------
    void OnEnable()
    {
      if (CarManagerEditor.IsOpen)
      {
        CarManagerEditor window = CarManagerEditor.Instance;
        window.WantRepaint += Repaint;
      }
    }
    //-----------------------------------------------------------------------------------
    void OnDisable()
    {
      if (CarManagerEditor.IsOpen)
      {
        CarManagerEditor window = CarManagerEditor.Instance;
        window.WantRepaint -= Repaint;
      }

      if (View != null)
      {
        View.Deinit();
      }
    }
    //-----------------------------------------------------------------------------------
    void OnDestroy()
    {
      Controller.UnselectSelected();
      if (View != null)
      {
        View.Deinit();
      }
    }
    //-----------------------------------------------------------------------------------
    void OnLostFocus()
    {
      if (Controller != null)
      {
        Controller.UnselectSelected();
        if (Controller.ItemIdxEditing != -1)
        {
          Controller.SetItemName(Controller.ItemIdxEditing, Controller.ItemIdxEditingName);
          Controller.ItemIdxEditing = -1;
        }
      }
    }   
    //-----------------------------------------------------------------------------------
    void OnGUI()
    {
      GUILayout.BeginArea ( new Rect(0, 0, position.width, position.height) );
      if (View != null)
      {
        View.RenderGUI( position );
      }
      GUILayout.EndArea ();
    }
    //-----------------------------------------------------------------------------------
    public static void Update()
    {
      if (IsOpen)
      {
        CNFieldController fController = Instance.Controller;
        if (fController != null)
        {
          fController.WantsUpdate();
        }

        Instance.Repaint();
      }
    }
    //-----------------------------------------------------------------------------------
    public static CNFieldWindow ShowWindow<T>(string windowName, CNFieldController controller, CommandNodeEditor ownerEditor)
      where T: CNFieldWindow
    {
      if (IsOpen)
      {
        Instance.Close();
      }

      Instance = EditorWindow.GetWindow<T>(true, windowName, true);
      Instance.Init( controller, ownerEditor );

      return (Instance);
    }
    //-----------------------------------------------------------------------------------
    public abstract void Init( CNFieldController controller, CommandNodeEditor ownerEditor );
    //-----------------------------------------------------------------------------------
  }
}

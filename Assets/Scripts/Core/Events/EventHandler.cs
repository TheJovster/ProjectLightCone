using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LightCone.Core.Events
{
    public class EventHandler : MonoBehaviour
    {
        public interface IEvent
        {
            void OnBegin(bool bFirstTime);
            void OnUpdate();
            void OnEnd();
            bool IsDone();
        }

        public abstract class GameEvent : IEvent
        {
            public virtual void OnBegin(bool bFirstTime)
            {
            }

            public virtual void OnUpdate()
            {
            }

            public virtual void OnEnd()
            {
            }

            public virtual bool IsDone()
            {
                return true;
            }
        }

        public abstract class GameEventBehaviour : MonoBehaviour, IEvent
        {
            public virtual void OnBegin(bool bFirstTime)
            {
            }

            public virtual void OnUpdate()
            {
            }

            public virtual void OnEnd()
            {
            }

            public virtual bool IsDone()
            {
                return true;
            }
        }

        private List<IEvent>            eventStack = new List<IEvent>();
        private HashSet<IEvent>         startedEvents = new HashSet<IEvent>();
        private IEvent                  currentEvent;

        private static EventHandler     main = null;

        #region Properties

        public IEvent CurrentEvent => currentEvent;

        public List<IEvent> EventStack => eventStack;

        public static EventHandler Main
        {
            get
            {
                if (main == null &&
                    Application.isPlaying)
                {
                    GameObject go = new GameObject("MainEventHandler");
                    DontDestroyOnLoad(go);
                    main = go.AddComponent<EventHandler>();
                }

                return main;
            }
        }

        #endregion

        public void PushEvent(IEvent evt)
        {
            if (evt != null)
            {
                // already on stack?
                eventStack.RemoveAll(e => e == evt);

                // insert event
                eventStack.Insert(0, evt);

                // reset current event?
                if (currentEvent != null && 
                    currentEvent != evt)
                {
                    currentEvent = null;
                }
            }
        }

        public void RemoveEvent(IEvent evt)
        {
            if (evt == null || !eventStack.Contains(evt))
            {
                return;
            }

            // call on end?
            if (evt == currentEvent ||
                startedEvents.Contains(evt))
            {
                evt.OnEnd();
                currentEvent = null;
            }

            // remove the event
            eventStack.Remove(evt);
        }

        protected virtual void Update()
        {
            UpdateEvents();
        }

        protected virtual void UpdateEvents()
        {
            if (eventStack.Count == 0)
            {
                return;
            }

            // pick a new current event?
            if (currentEvent == null)
            {
                // set current event
                startedEvents.RemoveWhere(evt => evt == null);
                currentEvent = eventStack[0];
                bool bFirstTime = !startedEvents.Contains(currentEvent);
                startedEvents.Add(currentEvent);
                currentEvent.OnBegin(bFirstTime);

                // did something affect the stack in the OnBegin()?
                if (eventStack != null)
                {
                    if (eventStack.Count > 0 && currentEvent != eventStack[0])
                    {
                        currentEvent = null;
                        UpdateEvents();
                    }
                }
            }

            // update current event
            if (currentEvent != null)
            {
                currentEvent.OnUpdate();

                // still the same event?
                if (eventStack.Count > 0 &&
                    currentEvent == eventStack[0])
                {
                    // did we finish the event?
                    if (currentEvent.IsDone())
                    {
                        eventStack.RemoveAt(0);
                        currentEvent.OnEnd();
                        startedEvents.Remove(currentEvent);
                        currentEvent = null;
                    }
                }
            }
        }

        protected virtual void OnGUI()
        {
            if (this != main)
            {
                return;
            }

            #if UNITY_EDITOR
            const float LINE_HEIGHT = 32.0f;

            GUI.color = new Color(0.0f, 0.0f, 0.0f, 0.7f);
            Rect r = new Rect(0, 0, 250.0f, LINE_HEIGHT * eventStack.Count);
            GUI.DrawTexture(r, Texture2D.whiteTexture);

            Rect line = new Rect(10, 0, r.width - 20, LINE_HEIGHT);
            for (int i = 0; i < eventStack.Count; i++)
            {
                GUI.color = eventStack[i] == currentEvent ? Color.green : Color.white;
                GUI.Label(line, "#" + i + ": " + eventStack[i].ToString(), i == 0 ? UnityEditor.EditorStyles.boldLabel : UnityEditor.EditorStyles.label);
                line.y += line.height;
            }
            #endif
        }
    }
}
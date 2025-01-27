/*
 * Copyright(c) 2022 Samsung Electronics Co., Ltd.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 *
 */
extern alias TizenSystemInformation;
using TizenSystemInformation.Tizen.System;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using Tizen.NUI.BaseComponents;

namespace Tizen.NUI
{
    public partial class Window
    {
        #region Constant Fields
        #endregion //Constant Fields

        #region Fields
        private IBorderInterface borderInterface = null;
        private Layer borderWindowRootLayer = null;
        private Layer borderWindowBottomLayer = null;

        // for border area
        private View rootView = null;
        private BorderView borderView = null;
        private View topView = null;
        private View contentsView = null;
        private View bottomView = null;
        private float borderHeight = 0;
        private int screenWidth = 0;
        private int screenHeight = 0;

        private bool isBorderWindow = false;
        private bool hasTopView = false;
        private bool hasBottomView = false;
        private bool isEnabledOverlayMode = false;
        private bool isMaximized = false;

        // for config
        private Size2D minSize = null;
        private Size2D maxSize = null;
        private BorderResizePolicyType borderResizePolicy = BorderResizePolicyType.Free;
        #endregion //Fields

        #region Constructors
        #endregion //Constructors

        #region Distructors
        #endregion //Distructors

        #region Delegates
        internal delegate void BorderCloseDelegate();
        private BorderCloseDelegate borderCloseDelegate = null;

        #endregion //Delegates

        #region Events
        #endregion //Events

        #region Enums
        /// <summary>
        /// This is an enum for the resize direction or move value when the border area is touched.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public enum BorderDirection
        {
            None        = ResizeDirection.None,
            TopLeft     = ResizeDirection.TopLeft,
            Top         = ResizeDirection.Top,
            TopRight    = ResizeDirection.TopRight,
            Left        = ResizeDirection.Left,
            Right       = ResizeDirection.Right,
            BottomLeft  = ResizeDirection.BottomLeft,
            Bottom      = ResizeDirection.Bottom,
            BottomRight = ResizeDirection.BottomRight,
            Move,
        }

        /// <summary>
        /// This enum is the policy when resizing the border window.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public enum BorderResizePolicyType
        {
          /// <summary>
          /// The window can be resized freely.
          /// </summary>
          Free = 0,
          /// <summary>
          /// The window is resized according to the ratio.
          /// </summary>
          KeepRatio = 1,
          /// <summary>
          /// The window is not resized and is fixed.
          /// </summary>
          Fixed = 2,
        }
        #endregion //Enums

        #region Interfaces
        #endregion //Interfaces

        #region Properties
        /// <summary>
        /// Whether the border is enabled.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool IsBorderEnabled => isBorderWindow;
        #endregion //Properties

        #region Indexers
        #endregion //Indexers

        #region Methods

        /// <summary>
        /// Update BorderProperty
        /// </summary>
        internal void UpdateProperty()
        {
            if (borderInterface != null)
            {
                float height = 0;
                if (hasTopView) height += topView.SizeHeight;
                if (hasBottomView) height += bottomView.SizeHeight;

                if (height != borderHeight)
                {
                    float diff = height - borderHeight;
                    borderHeight = height;
                    WindowSize = new Size2D(WindowSize.Width, WindowSize.Height + (int)(diff));
                }

                if (minSize != borderInterface.MinSize)
                {
                    using Size2D mimimumSize = new Size2D(borderInterface.MinSize.Width, borderInterface.MinSize.Height + (int)borderHeight);
                    SetMimimumSize(mimimumSize);
                    minSize = borderInterface.MinSize;
                }
                if (maxSize != borderInterface.MaxSize)
                {
                    using Size2D maximumSize = new Size2D(borderInterface.MaxSize.Width, borderInterface.MaxSize.Height + (int)borderHeight);
                    SetMaximumSize(maximumSize);
                    maxSize = borderInterface.MaxSize;
                }
                if (borderResizePolicy != borderInterface.ResizePolicy)
                {
                    AddAuxiliaryHint("wm.policy.win.resize_aspect_ratio", "0");
                    borderResizePolicy = borderInterface.ResizePolicy;
                    if (borderResizePolicy == BorderResizePolicyType.KeepRatio)
                    {
                        AddAuxiliaryHint("wm.policy.win.resize_aspect_ratio", "1");
                    }
                }
            }
        }
        /// <summary>
        /// Called when the border is closed.
        /// If the delegate is declared, the delegate is called, otherwise window is destroyed.
        /// </summary>
        internal void BorderDestroy()
        {
            if (borderCloseDelegate != null)
            {
                borderCloseDelegate();
            }
            else
            {
                Destroy();
            }
        }
        /// <summary>
        /// Enable the border window with IBorderInterface.
        /// This adds a border area to the Window.
        /// The border's UI is configured using IBorderInterface.
        /// Users can reisze and move by touching the border area.
        /// </summary>
        /// <param name="borderInterface">The IBorderInterface.</param>
        /// <param name="borderCloseDelegate">The BorderCloseDelegate. When close, this delegate is called.</param>
        /// <returns>Whether the border window is enabled</returns>
        internal bool EnableBorder(IBorderInterface borderInterface, BorderCloseDelegate borderCloseDelegate = null)
        {
            if (isBorderWindow == true)
            {
                Tizen.Log.Error("NUI", $"Already EnableBorderWindow\n");
                return false;
            }

            try
            {
                Information.TryGetValue<int>("http://tizen.org/feature/screen.width", out screenWidth);
                Information.TryGetValue<int>("http://tizen.org/feature/screen.height", out screenHeight);
            }
            catch (DllNotFoundException e)
            {
                Tizen.Log.Fatal("NUI", $"{e}\n");
            }

            if (borderInterface == null)
            {
                borderInterface = new DefaultBorder();
            }
            this.borderInterface = borderInterface;
            this.borderCloseDelegate = borderCloseDelegate;

            GetDefaultLayer().Name = "OriginalRootLayer";

            SetTransparency(true);
            BackgroundColor = Color.Transparent;
            borderInterface.BorderWindow = this;

            if (CreateBorder() == true)
            {
                using var realWindowSize = new Size2D(WindowSize.Width, WindowSize.Height);

                isBorderWindow = true;

                Resized += OnBorderWindowResized;

                Moved += OnBorderWindowMoved;

                borderInterface.OnCreated(borderView);

                // Increase the window size as much as the border area.
                borderHeight = 0;
                if (hasTopView) borderHeight += topView.SizeHeight;
                if (hasBottomView) borderHeight += bottomView.SizeHeight;

                // Sets the minimum / maximum size to be resized by RequestResizeToServer.
                if (borderInterface.MinSize != null)
                {
                    using Size2D mimimumSize = new Size2D(borderInterface.MinSize.Width, borderInterface.MinSize.Height + (int)borderHeight);
                    SetMimimumSize(mimimumSize);
                }
                if (borderInterface.MaxSize != null)
                {
                    using Size2D maximumSize = new Size2D(borderInterface.MaxSize.Width, borderInterface.MaxSize.Height + (int)borderHeight);
                    SetMaximumSize(maximumSize);
                }

                // When running the app for the first time, if it runs in full size, do Maximize(true).
                if (screenWidth != 0 && screenHeight != 0 &&
                    realWindowSize.Width >= screenWidth && realWindowSize.Height >= screenHeight &&
                    IsMaximized() == false)
                {
                    Maximize(true);
                    borderInterface.OnMaximize(true);
                    ResizedEventArgs e = new ResizedEventArgs();
                    e.WindowSize = WindowSize;
                    OnBorderWindowResized(this, e);
                }
                else
                {
                    WindowSize += new Size2D((int)borderInterface.BorderLineThickness * 2, (int)(borderHeight + borderInterface.BorderLineThickness * 2));
                }

                // If it is BorderResizePolicyType.KeepRatio type, it will be resized according to the ratio.
                if (borderInterface.ResizePolicy == BorderResizePolicyType.KeepRatio)
                {
                    AddAuxiliaryHint("wm.policy.win.resize_aspect_ratio", "1");
                }

                // Add a view to the border layer.
                GetBorderWindowBottomLayer().Add(rootView);

                FocusChanged += OnWindowFocusChanged;

                return true;
            }
            else
            {
                this.borderInterface.Dispose();
                return false;
            }
        }

        private void OnWindowFocusChanged(object sender, Window.FocusChangedEventArgs e)
        {
            if (e.FocusGained == true && IsMaximized() == false)
            {
                // Raises the window when the window is focused.
                Raise();
            }
        }

        /// Create the border UI.
        private bool CreateBorder()
        {
            rootView = new View()
            {
                WidthResizePolicy = ResizePolicyType.FillToParent,
                HeightResizePolicy = ResizePolicyType.FillToParent,
                BackgroundColor = Color.Transparent,
            };

            ushort padding = (ushort) borderInterface.BorderLineThickness;
            borderView = new BorderView()
            {
                GrabTouchAfterLeave = true,
                WidthResizePolicy = ResizePolicyType.FillToParent,
                HeightResizePolicy = ResizePolicyType.FillToParent,
                BackgroundColor = Color.Transparent,
                Layout = new LinearLayout() {
                    LinearOrientation = LinearLayout.Orientation.Vertical,
                    LinearAlignment = LinearLayout.Alignment.Top
                },
                Padding = new Extents(padding, padding, padding, padding),
            };
            borderInterface.CreateBorderView(borderView);

            topView = new View()
            {
                WidthSpecification = LayoutParamPolicies.MatchParent,
                SizeHeight = borderInterface.BorderHeight,
                BackgroundColor = Color.Transparent,
            };

            contentsView = new View()
            {
                BackgroundColor = Color.Transparent,
                WidthSpecification = LayoutParamPolicies.MatchParent,
            };

            bottomView = new View()
            {
                WidthSpecification = LayoutParamPolicies.MatchParent,
                SizeHeight = borderInterface.BorderHeight,
                BackgroundColor = Color.Transparent,
            };

            // // Gets the Border's UI.
            if (borderInterface.CreateTopBorderView(topView) == true && topView != null)
            {
                borderView.Add(topView);
                hasTopView = true;
            }
            borderView.Add(contentsView);
            if (borderInterface.CreateBottomBorderView(bottomView) == true && bottomView != null)
            {
                borderView.Add(bottomView);
                hasBottomView = true;
            }
            rootView.Add(borderView);

            return hasTopView || hasBottomView;
        }

        /// <summary>
        /// Calculates which direction to resize or to move.
        /// </summary>
        /// <param name="xPosition">The X position.</param>
        /// <param name="yPosition">The Y position.</param>
        /// <returns>The BorderDirection</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public BorderDirection GetDirection(float xPosition, float yPosition)
        {
            BorderDirection direction = BorderDirection.None;

            // check bottom left corner
            if (xPosition < borderInterface.TouchThickness && yPosition > WindowSize.Height + borderHeight - borderInterface.TouchThickness)
            {
                direction = BorderDirection.BottomLeft;
            }
            // check bottom right corner
            else if (xPosition > WindowSize.Width + (float)(borderInterface.BorderLineThickness * 2) - borderInterface.TouchThickness && yPosition > WindowSize.Height + borderHeight - borderInterface.TouchThickness)
            {
                direction = BorderDirection.BottomRight;
            }
            // check top left corner
            else if (xPosition < borderInterface.TouchThickness && yPosition <  borderInterface.TouchThickness)
            {
                direction = BorderDirection.TopLeft;
            }
            // check top right corner
            else if (xPosition > WindowSize.Width + (float)(borderInterface.BorderLineThickness * 2) - borderInterface.TouchThickness && yPosition < borderInterface.TouchThickness)
            {
                direction = BorderDirection.TopRight;
            }
            // check left side
            else if (xPosition < borderInterface.TouchThickness)
            {
                direction = BorderDirection.Left;
            }
            // check right side
            else if (xPosition > WindowSize.Width + (float)(borderInterface.BorderLineThickness * 2) - borderInterface.TouchThickness)
            {
                direction = BorderDirection.Right;
            }
            // check bottom side
            else if (yPosition > WindowSize.Height + borderHeight + borderInterface.BorderLineThickness - borderInterface.TouchThickness)
            {
                direction = BorderDirection.Bottom;
            }
            // check top side
            else if (yPosition < borderInterface.TouchThickness)
            {
                direction = BorderDirection.Top;
            }
            // check move
            else if ((yPosition > WindowSize.Height) || (hasTopView == true && yPosition < topView.SizeHeight))
            {
                direction = BorderDirection.Move;
            }

            return direction;
        }

        private void DoOverlayMode(bool enable)
        {
            if (borderInterface.OverlayMode == true)
            {
                if (isEnabledOverlayMode != enable)
                {
                    borderView?.OverlayMode(enable);
                    borderInterface.OnOverlayMode(enable);
                    if (enable == true)
                    {
                        GetBorderWindowBottomLayer().RaiseToTop();
                    }
                    else
                    {
                        GetBorderWindowBottomLayer().LowerToBottom();
                    }
                    isEnabledOverlayMode = enable;
                }
            }
        }

        private void DoMaximize(bool isMaximized)
        {
            if (this.isMaximized != isMaximized)
            {
                borderView?.OnMaximize(isMaximized);
                borderInterface.OnMaximize(isMaximized);
            }
            this.isMaximized = isMaximized;
        }

        // Called when the window position has changed.
        private void OnBorderWindowMoved(object sender, WindowMovedEventArgs e)
        {
            Tizen.Log.Info("NUI", $"OnBorderWindowMoved {e.WindowPosition.X}, {e.WindowPosition.Y}\n");
            borderInterface.OnMoved(e.WindowPosition.X, e.WindowPosition.X);
        }


        // Called when the window size has changed.
        private void OnBorderWindowResized(object sender, Window.ResizedEventArgs e)
        {
            Tizen.Log.Info("NUI", $"OnBorderWindowResized {e.WindowSize.Width},{e.WindowSize.Height}\n");
            int resizeWidth = e.WindowSize.Width;
            int resizeHeight = e.WindowSize.Height;

            borderInterface.OnResized(resizeWidth, resizeHeight);

             // reset borderHeight
            borderHeight = 0;
            if (hasTopView) borderHeight += topView.SizeHeight;
            if (hasBottomView) borderHeight += bottomView.SizeHeight;

            bool isMaximized = IsMaximized();
            bool isEnabledOverlay = (borderInterface.OverlayMode == true && isMaximized == true);

            float width = 0;
            float height = isEnabledOverlay ? 0 : borderHeight;
            float y = isEnabledOverlay ? 0 : ((hasTopView == true) ? topView.SizeHeight : 0);

            if (isMaximized == false)
            {
                width = (float)(borderInterface.BorderLineThickness * 2);
                height += (float)(borderInterface.BorderLineThickness * 2);
                y += borderInterface.BorderLineThickness;
            }

            Interop.ActorInternal.SetSize(GetBorderWindowRootLayer().SwigCPtr, resizeWidth, resizeHeight);
            Interop.ActorInternal.SetSize(GetBorderWindowBottomLayer().SwigCPtr, resizeWidth + width, resizeHeight + height);
            Interop.ActorInternal.SetPosition(GetBorderWindowRootLayer().SwigCPtr, 0, y);

            if (contentsView != null)
            {
                contentsView.SizeHeight = resizeHeight - (isEnabledOverlay ? borderHeight : 0);
            }
            DoMaximize(isMaximized);

            DoOverlayMode(isEnabledOverlay);

            if (NDalicPINVOKE.SWIGPendingException.Pending) { throw NDalicPINVOKE.SWIGPendingException.Retrieve(); }
        }

        internal Layer GetBorderWindowBottomLayer()
        {
            if (borderWindowBottomLayer == null)
            {
                borderWindowBottomLayer = new Layer();
                borderWindowBottomLayer.Name = "BorderWindowBottomLayer";
                using Vector3 topCentor = new Vector3(0.5f, 0.0f, 0.5f);
                Interop.ActorInternal.SetParentOrigin(borderWindowBottomLayer.SwigCPtr, topCentor.SwigCPtr);
                Interop.Actor.SetAnchorPoint(borderWindowBottomLayer.SwigCPtr, topCentor.SwigCPtr);
                Interop.Actor.Add(rootLayer.SwigCPtr, borderWindowBottomLayer.SwigCPtr);
                Interop.ActorInternal.SetSize(borderWindowBottomLayer.SwigCPtr, WindowSize.Width + (float)(borderInterface.BorderLineThickness * 2), WindowSize.Height + (float)(borderInterface.BorderLineThickness * 2));
                borderWindowBottomLayer.SetWindow(this);
                borderWindowBottomLayer.LowerToBottom();

                if (NDalicPINVOKE.SWIGPendingException.Pending) { throw NDalicPINVOKE.SWIGPendingException.Retrieve(); }
            }
            return borderWindowBottomLayer;
        }

        internal Layer GetBorderWindowRootLayer()
        {
            if (borderWindowRootLayer == null)
            {
                borderWindowRootLayer = new Layer();
                borderWindowRootLayer.Name = "RootLayer";
                using Vector3 topCentor = new Vector3(0.5f, 0.0f, 0.5f);
                Interop.ActorInternal.SetParentOrigin(borderWindowRootLayer.SwigCPtr, topCentor.SwigCPtr);
                Interop.Actor.SetAnchorPoint(borderWindowRootLayer.SwigCPtr, topCentor.SwigCPtr);
                Interop.Actor.Add(rootLayer.SwigCPtr, borderWindowRootLayer.SwigCPtr);
                Interop.ActorInternal.SetSize(borderWindowRootLayer.SwigCPtr, WindowSize.Width, WindowSize.Height - borderHeight - borderInterface.BorderLineThickness * 2);
                float height = (hasTopView == true) ? topView.SizeHeight : 0;
                Interop.ActorInternal.SetPosition(borderWindowRootLayer.SwigCPtr, 0, height + borderInterface.BorderLineThickness);
                using PropertyValue propertyValue = new Tizen.NUI.PropertyValue((int)Tizen.NUI.ClippingModeType.ClipToBoundingBox);
                Tizen.NUI.Object.SetProperty(borderWindowRootLayer.SwigCPtr, Tizen.NUI.BaseComponents.View.Property.ClippingMode, propertyValue);

                if (NDalicPINVOKE.SWIGPendingException.Pending) { throw NDalicPINVOKE.SWIGPendingException.Retrieve(); }
            }

            return borderWindowRootLayer;
        }

        internal void DisposeBorder()
        {
            Resized -= OnBorderWindowResized;
            FocusChanged -= OnWindowFocusChanged;
            borderInterface.Dispose();
            GetBorderWindowBottomLayer().Dispose();
        }

        private void convertBorderWindowSizeToRealWindowSize(Uint16Pair size)
        {
            if (isBorderWindow == true)
            {
                var height = (ushort)(size.GetHeight() + borderHeight + borderInterface.BorderLineThickness * 2);
                var width = (ushort)(size.GetWidth() + borderInterface.BorderLineThickness * 2);
                size.SetHeight(height);
                size.SetWidth(width);
            }
        }

        private void convertRealWindowSizeToBorderWindowSize(Uint16Pair size)
        {
            if (isBorderWindow == true && !(borderInterface.OverlayMode == true && IsMaximized() == true))
            {
                var height = (ushort)(size.GetHeight() - borderHeight - borderInterface.BorderLineThickness * 2);
                var width = (ushort)(size.GetWidth() - borderInterface.BorderLineThickness * 2);
                size.SetHeight(height);
                size.SetWidth(width);
            }
        }
        #endregion //Methods

        #region Structs
        #endregion //Structs

        #region Classes
        // View class for border view.
        private class BorderView : View
        {
            private bool isEnabledOverlay = false;
            private Extents prePadding = new Extents(0, 0, 0, 0);

            /// <summary>
            /// Set whether or not it is in overlay mode.
            /// The borderView's OverlayMode means that it is displayed at the top of the screen.
            /// In this case, the borderView should pass the event so that lower layers can receive the event.
            /// </summary>
            /// <param name="enabled">The true if borderView is Overlay mode. false otherwise.</param>
            [EditorBrowsable(EditorBrowsableState.Never)]
            public void OverlayMode(bool enabled)
            {
                isEnabledOverlay = enabled;
            }

            /// <summary>
            /// Called when the window is maximized.
            /// </summary>
            /// <param name="isMaximized">If window is maximized or unmaximized.</param>
            [EditorBrowsable(EditorBrowsableState.Never)]
            public void OnMaximize(bool isMaximized)
            {
                // When maximized, it is displayed in full without border lines.
                if (isMaximized == true)
                {
                    prePadding = Padding;
                    Padding = new Extents(0, 0, 0, 0);
                }
                else
                {
                    Padding = prePadding;
                }
                
            }

            protected override bool HitTest(Touch touch)
            {
                // If borderView is in overlay mode, pass the hittest.
                return (isEnabledOverlay == false);
            }
        }
        #endregion //Classes
    }
}

/* Licensed under the MIT/X11 license.
 * Copyright (c) 2006-2008 the OpenTK Team.
 * This notice may not be removed from any source distribution.
 * See license.txt for licensing detailed licensing details.
 */

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

using OpenTK.Platform;
namespace OpenTK.Graphics
{
    /// <summary>
    /// Represents and provides methods to manipulate an OpenGL render context.
    /// </summary>
    public sealed class GraphicsContext : IGraphicsContext, IGraphicsContextInternal
    {

        IGraphicsContext implementation;  // The actual render context implementation for the underlying platform.
        bool disposed;
        // Indicates that this context was created through external means, e.g. Tao.Sdl or GLWidget#.
        // In this case, We'll assume that the external program will manage the lifetime of this
        // context - we'll not destroy it manually.
        readonly bool IsExternal;
        bool check_errors = true;
        static bool share_contexts = true;
        static bool direct_rendering = true;
        readonly static object SyncRoot = new object();
        // Maps OS-specific context handles to GraphicsContext weak references.
        readonly static Dictionary<ContextHandle, WeakReference> available_contexts = new Dictionary<ContextHandle, WeakReference>();



        //// Necessary to allow creation of dummy GraphicsContexts (see CreateDummyContext static method).
        //GraphicsContext(ContextHandle handle)
        //{
        //    implementation = new OpenTK.Platform.Dummy.DummyGLContext(handle);
        //    lock (SyncRoot)
        //    {
        //        available_contexts.Add((implementation as IGraphicsContextInternal).Context, new WeakReference(this));
        //    }
        //}

        /// <summary>
        /// Constructs a new GraphicsContext with the specified GraphicsMode and attaches it to the specified window.
        /// </summary>
        /// <param name="mode">The OpenTK.Graphics.GraphicsMode of the GraphicsContext.</param>
        /// <param name="window">The OpenTK.Platform.IWindowInfo to attach the GraphicsContext to.</param>
        public GraphicsContext(GraphicsMode mode, IWindowInfo window)
            : this(mode, window, 1, 0, GraphicsContextFlags.Default)
        { }

        /// <summary>
        /// Constructs a new GraphicsContext with the specified GraphicsMode, version and flags,  and attaches it to the specified window.
        /// </summary>
        /// <param name="mode">The OpenTK.Graphics.GraphicsMode of the GraphicsContext.</param>
        /// <param name="window">The OpenTK.Platform.IWindowInfo to attach the GraphicsContext to.</param>
        /// <param name="major">The major version of the new GraphicsContext.</param>
        /// <param name="minor">The minor version of the new GraphicsContext.</param>
        /// <param name="flags">The GraphicsContextFlags for the GraphicsContext.</param>
        /// <remarks>
        /// Different hardware supports different flags, major and minor versions. Invalid parameters will be silently ignored.
        /// </remarks>
        public GraphicsContext(GraphicsMode mode, IWindowInfo window, int major, int minor, GraphicsContextFlags flags)
        {
            lock (SyncRoot)
            {
                bool designMode = false;
                if (mode == null && window == null)
                    designMode = true;
                else if (mode == null) throw new ArgumentNullException("mode", "Must be a valid GraphicsMode.");
                else if (window == null) throw new ArgumentNullException("window", "Must point to a valid window.");
                // Silently ignore invalid major and minor versions.
                if (major <= 0)
                    major = 1;
                if (minor < 0)
                    minor = 0;
                Debug.Write("Creating GraphicsContext.");
                try
                {
                    //Debug.Indent();
                    Debug.Write(string.Format("GraphicsMode: {0}", mode));
                    Debug.Write(string.Format("IWindowInfo: {0}", window));
                    Debug.Write(string.Format("GraphicsContextFlags: {0}", flags));
                    Debug.Write(string.Format("Requested version: {0}.{1}", major, minor));
                    IGraphicsContext shareContext = shareContext = FindSharedContext();
                    // Todo: Add a DummyFactory implementing IPlatformFactory.
                    if (designMode)
                    {
                        implementation = new Platform.Dummy.DummyGLContext();
                    }
                    else
                    {
                        IPlatformFactory factory = null;
                        switch ((flags & GraphicsContextFlags.Embedded) == GraphicsContextFlags.Embedded)
                        {
                            case false: factory = Factory.Default; break;
                            case true: factory = Factory.Embedded; break;
                        }

                        implementation = factory.CreateGLContext(mode, window, shareContext, direct_rendering, major, minor, flags);
                        // Note: this approach does not allow us to mix native and EGL contexts in the same process.
                        // This should not be a problem, as this use-case is not interesting for regular applications.
                        // Note 2: some platforms may not support a direct way of getting the current context
                        // (this happens e.g. with DummyGLContext). In that case, we use a slow fallback which
                        // iterates through all known contexts and checks if any is current (check GetCurrentContext
                        // declaration).
                        if (GetCurrentContext == null)
                        {
                            GetCurrentContextDelegate temp = factory.CreateGetCurrentGraphicsContext();
                            if (temp != null)
                            {
                                GetCurrentContext = temp;
                            }
                        }
                    }

                    available_contexts.Add((this as IGraphicsContextInternal).Context, new WeakReference(this));
                }
                catch (Exception ex)
                {
                }
                finally
                {
                    //Debug.Unindent();
                }
            }
        }





        ///// <summary>
        ///// Constructs a new GraphicsContext from a pre-existing context created outside of OpenTK.
        ///// </summary>
        ///// <param name="handle">The handle of the existing context. This must be a valid, unique handle that is not known to OpenTK.</param>
        ///// <param name="window">The window this context is bound to. This must be a valid window obtained through Utilities.CreateWindowInfo.</param>
        ///// <exception cref="GraphicsContextException">Occurs if handle is identical to a context already registered with OpenTK.</exception>
        //public GraphicsContext(ContextHandle handle, IWindowInfo window)
        //    : this(handle, window, null, 1, 0, GraphicsContextFlags.Default)
        //{ }

        /// <summary>
        /// Constructs a new GraphicsContext from a pre-existing context created outside of OpenTK.
        /// </summary>
        /// <param name="handle">The handle of the existing context. This must be a valid, unique handle that is not known to OpenTK.</param>
        /// <param name="window">The window this context is bound to. This must be a valid window obtained through Utilities.CreateWindowInfo.</param>
        /// <param name="shareContext">A different context that shares resources with this instance, if any.
        /// Pass null if the context is not shared or if this is the first GraphicsContext instruct you construct.</param>
        /// <param name="major">The major version of the context (e.g. "2" for "2.1").</param>
        /// <param name="minor">The minor version of the context (e.g. "1" for "2.1").</param>
        /// <param name="flags">A bitwise combination of <see cref="GraphicsContextFlags"/> that describe this context.</param>
        /// <exception cref="GraphicsContextException">Occurs if handle is identical to a context already registered with OpenTK.</exception>
        //public GraphicsContext(ContextHandle handle, IWindowInfo window, IGraphicsContext shareContext, int major, int minor, GraphicsContextFlags flags)
        //{
        //    lock (SyncRoot)
        //    {
        //        IsExternal = true;
        //        if (handle == ContextHandle.Zero)
        //        {
        //            implementation = new OpenTK.Platform.Dummy.DummyGLContext(handle);
        //        }
        //        else if (available_contexts.ContainsKey(handle))
        //        {
        //            throw new GraphicsContextException("Context already exists.");
        //        }
        //        else
        //        {
        //            switch ((flags & GraphicsContextFlags.Embedded) == GraphicsContextFlags.Embedded)
        //            {
        //                case false: implementation = Factory.Default.CreateGLContext(handle, window, shareContext, direct_rendering, major, minor, flags); break;
        //                case true: implementation = Factory.Embedded.CreateGLContext(handle, window, shareContext, direct_rendering, major, minor, flags); break;
        //            }
        //        }

        //        available_contexts.Add((implementation as IGraphicsContextInternal).Context, new WeakReference(this));
        //        (this as IGraphicsContextInternal).LoadAll();
        //    }
        //}





        static IGraphicsContext FindSharedContext()
        {
            if (GraphicsContext.ShareContexts)
            {
                // A small hack to create a shared context with the first available context.
                foreach (WeakReference r in GraphicsContext.available_contexts.Values)
                {
                    // Fix for bug 1874: if a GraphicsContext gets finalized
                    // (but not disposed), it won't be removed from available_contexts
                    // making this return null even if another valid context exists.
                    // The workaround is to simply ignore null targets.
                    IGraphicsContext target = r.Target as IGraphicsContext;
                    if (target != null)
                        return target;
                }
            }
            return null;
        }


        ///// <summary>
        ///// Creates a dummy GraphicsContext to allow OpenTK to work with contexts created by external libraries.
        ///// </summary>
        ///// <returns>A new, dummy GraphicsContext instance.</returns>
        ///// <remarks>
        ///// <para>Instances created by this method will not be functional. Instance methods will have no effect.</para>
        ///// <para>This method requires that a context is current on the calling thread.</para>
        ///// </remarks>
        //public static GraphicsContext CreateDummyContext()
        //{
        //    ContextHandle handle = GetCurrentContext();
        //    if (handle == ContextHandle.Zero)
        //        throw new InvalidOperationException("No GraphicsContext is current on the calling thread.");
        //    return CreateDummyContext(handle);
        //}

        ///// <summary>
        ///// Creates a dummy GraphicsContext to allow OpenTK to work with contexts created by external libraries.
        ///// </summary>
        ///// <param name="handle">The handle of a context.</param>
        ///// <returns>A new, dummy GraphicsContext instance.</returns>
        ///// <remarks>
        ///// <para>Instances created by this method will not be functional. Instance methods will have no effect.</para>
        ///// </remarks>
        //public static GraphicsContext CreateDummyContext(ContextHandle handle)
        //{
        //    if (handle == ContextHandle.Zero)
        //        throw new ArgumentOutOfRangeException("handle");
        //    return new GraphicsContext(handle);
        //}

        /// <summary>
        /// Checks if a GraphicsContext exists in the calling thread and throws a GraphicsContextMissingException if it doesn't.
        /// </summary>
        /// <exception cref="GraphicsContextMissingException">Generated when no GraphicsContext is current in the calling thread.</exception>
        public static void Assert()
        {
            if (GraphicsContext.CurrentContext == null)
                throw new GraphicsContextMissingException();
        }



        ///////////////////////////////////////////////////////////////////////////////////////////////////////////
        //TODO: review here, this is our custom OpenGLContext
        //eg, used with glfw
        GraphicsContext(OpenTK.Platform.External.ExternalGraphicsContext externalGraphicsContext)
        {
            implementation = externalGraphicsContext;
            lock (SyncRoot)
            {
                IsExternal = true;
                //if (handle == ContextHandle.Zero)
                //{
                //    implementation = new OpenTK.Platform.Dummy.DummyGLContext(handle);
                //}
                //else if (available_contexts.ContainsKey(handle))
                //{
                //    throw new GraphicsContextException("Context already exists.");
                //}
                //else
                //{
                //    switch ((flags & GraphicsContextFlags.Embedded) == GraphicsContextFlags.Embedded)
                //    {
                //        case false: implementation = Factory.Default.CreateGLContext(handle, window, shareContext, direct_rendering, major, minor, flags); break;
                //        case true: implementation = Factory.Embedded.CreateGLContext(handle, window, shareContext, direct_rendering, major, minor, flags); break;
                //    }
                //}

                available_contexts.Add((implementation as IGraphicsContextInternal).Context, new WeakReference(this));
                (this as IGraphicsContextInternal).LoadAll();


                GetCurrentContextDelegate temp = externalGraphicsContext.CreateCurrentContextDel();
                if (temp != null)
                {
                    GetCurrentContext = temp;
                }


            }
        }

        /// <summary>
        /// TODO: for used with glfw context
        /// </summary>
        /// <param name="handle"></param>
        /// <returns></returns>
        public static GraphicsContext CreateExternalContext(Platform.External.ExternalGraphicsContext externalGfxContext)
        {
            return new GraphicsContext(externalGfxContext);
        }
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////
        //---------------------------
        //TODO: review here ****
        //---------------------------
        public delegate ContextHandle GetCurrentContextDelegate();
        internal static GetCurrentContextDelegate GetCurrentContext = delegate
        {
            // Note: this is a slow, generic fallback for use with DummyGLContext.
            // Most other platforms can query the current context directly (via
            // [Wgl|Glx|Agl|Egl].GetCurrentContext()) so the GraphicsContext
            // constructor will replace this implementation with a platform-specific
            // one, if it exists.
            foreach (WeakReference weak_ref in available_contexts.Values)
            {
                IGraphicsContext context = (IGraphicsContext)weak_ref.Target;
                if (context.IsCurrent)
                {
                    return (context as IGraphicsContextInternal).Context;
                }
            }
            return ContextHandle.Zero;
        };
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////
        //--------------------------------------------------------------------------------------

        /// <summary>
        /// Gets the GraphicsContext that is current in the calling thread.
        /// </summary>
        /// <remarks>
        /// Note: this property will not function correctly when both desktop and EGL contexts are
        /// available in the same process. This scenario is very unlikely to appear in practice.
        /// </remarks>
        public static IGraphicsContext CurrentContext
        {
            get
            {
                lock (SyncRoot)
                {
                    if (available_contexts.Count > 0)
                    {
                        ContextHandle handle = GetCurrentContext();
                        if (handle.Handle != IntPtr.Zero)
                        {

                            object target = available_contexts[handle].Target;
                            return (GraphicsContext)target;
                            //return (GraphicsContext)available_contexts[handle].Target;
                        }
                    }
                    return null;
                }
            }
        }

        /// <summary>Gets or sets a System.Boolean, indicating whether GraphicsContext resources are shared</summary>
        /// <remarks>
        /// <para>If ShareContexts is true, new GLContexts will share resources. If this value is
        /// false, new GLContexts will not share resources.</para>
        /// <para>Changing this value will not affect already created GLContexts.</para>
        /// </remarks>
        public static bool ShareContexts { get { return share_contexts; } set { share_contexts = value; } }


        /// <summary>Gets or sets a System.Boolean, indicating whether GraphicsContexts will perform direct rendering.</summary>
        /// <remarks>
        /// <para>
        /// If DirectRendering is true, new contexts will be constructed with direct rendering capabilities, if possible.
        /// If DirectRendering is false, new contexts will be constructed with indirect rendering capabilities.
        /// </para>
        /// <para>This property does not affect existing GraphicsContexts, unless they are recreated.</para>
        /// <para>
        /// This property is ignored on Operating Systems without support for indirect rendering, like Windows and OS X.
        /// </para>
        /// </remarks>
        public static bool DirectRendering
        {
            get { return direct_rendering; }
            set { direct_rendering = value; }
        }


        /// <summary>
        /// Gets or sets a System.Boolean, indicating whether automatic error checking should be performed.
        /// Influences the debug version of OpenTK.dll, only.
        /// </summary>
        /// <remarks>Automatic error checking will clear the OpenGL error state. Set CheckErrors to false if you use
        /// the OpenGL error state in your code flow (e.g. for checking supported texture formats).</remarks>
        public bool ErrorChecking
        {
            get { return check_errors; }
            set { check_errors = value; }
        }
        /// <summary>
        /// Creates an OpenGL context with the specified direct/indirect rendering mode and sharing state with the
        /// specified IGraphicsContext.
        /// </summary>
        /// <param name="direct">Set to true for direct rendering or false otherwise.</param>
        /// <param name="source">The source IGraphicsContext to share state from.</param>.
        /// <remarks>
        /// <para>
        /// Direct rendering is the default rendering mode for OpenTK, since it can provide higher performance
        /// in some circumastances.
        /// </para>
        /// <para>
        /// The 'direct' parameter is a hint, and will ignored if the specified mode is not supported (e.g. setting
        /// indirect rendering on Windows platforms).
        /// </para>
        /// </remarks>
        void CreateContext(bool direct, IGraphicsContext source)
        {
            lock (SyncRoot)
            {
                available_contexts.Add((this as IGraphicsContextInternal).Context, new WeakReference(this));
            }
        }

        /// <summary>
        /// Swaps buffers on a context. This presents the rendered scene to the user.
        /// </summary>
        public void SwapBuffers()
        {
            implementation.SwapBuffers();
        }

        /// <summary>
        /// Makes the GraphicsContext the current rendering target.
        /// </summary>
        /// <param name="window">A valid <see cref="OpenTK.Platform.IWindowInfo" /> structure.</param>
        /// <remarks>
        /// You can use this method to bind the GraphicsContext to a different window than the one it was created from.
        /// </remarks>
        public void MakeCurrent(IWindowInfo window)
        {
            implementation.MakeCurrent(window);
        }

        /// <summary>
        /// Gets a <see cref="System.Boolean"/> indicating whether this instance is current in the calling thread.
        /// </summary>
        public bool IsCurrent
        {
            get { return implementation.IsCurrent; }
        }

        /// <summary>
        /// Gets a <see cref="System.Boolean"/> indicating whether this instance has been disposed.
        /// It is an error to access any instance methods if this property returns true.
        /// </summary>
        public bool IsDisposed
        {
            get { return disposed && implementation.IsDisposed; }
            private set { disposed = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether VSync is enabled.
        /// </summary>
        public bool VSync
        {
            get { return implementation.VSync; }
            set { implementation.VSync = value; }
        }

        /// <summary>
        /// Updates the graphics context.  This must be called when the render target
        /// is resized for proper behavior on Mac OS X.
        /// </summary>
        /// <param name="window"></param>
        public void Update(IWindowInfo window)
        {
            implementation.Update(window);
        }

        /// <summary>
        /// Loads all OpenGL entry points.
        /// </summary>
        /// <exception cref="OpenTK.Graphics.GraphicsContextException">
        /// Occurs when this instance is not current on the calling thread.
        /// </exception>
        public void LoadAll()
        {
            if (GraphicsContext.CurrentContext != this)
                throw new GraphicsContextException();
            implementation.LoadAll();
        }


        /// <summary>
        /// Gets the platform-specific implementation of this IGraphicsContext.
        /// </summary>
        IGraphicsContext IGraphicsContextInternal.Implementation
        {
            get { return implementation; }
        }

        /// <summary>
        /// Gets a handle to the OpenGL rendering context.
        /// </summary>
        ContextHandle IGraphicsContextInternal.Context
        {
            get { return ((IGraphicsContextInternal)implementation).Context; }
        }

        /// <summary>
        /// Gets the GraphicsMode of the context.
        /// </summary>
        public GraphicsMode GraphicsMode
        {
            get { return (implementation as IGraphicsContext).GraphicsMode; }
        }

        /// <summary>
        /// Gets the address of an OpenGL extension function.
        /// </summary>
        /// <param name="function">The name of the OpenGL function (e.g. "glGetString")</param>
        /// <returns>
        /// A pointer to the specified function or IntPtr.Zero if the function isn't
        /// available in the current opengl context.
        /// </returns>
        IntPtr IGraphicsContextInternal.GetAddress(string function)
        {
            return (implementation as IGraphicsContextInternal).GetAddress(function);
        }


        /// <summary>
        /// Disposes of the GraphicsContext.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        void Dispose(bool manual)
        {
            if (!IsDisposed)
            {
                Debug.Write(
                    string.Format("Disposing context {0}.", (this as IGraphicsContextInternal).Context.ToString()));
                lock (SyncRoot)
                {
                    available_contexts.Remove((this as IGraphicsContextInternal).Context);
                }

                if (manual && !IsExternal)
                {
                    if (implementation != null)
                        implementation.Dispose();
                }
                IsDisposed = true;
            }
        }


    }
}
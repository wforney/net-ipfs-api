// <copyright file="AsyncLazy.cs" company="Richard Schneider">© 2015-2018 Richard Schneider</copyright>
namespace Ipfs.Api
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;

    /// <summary>Class AsyncLazy.</summary>
    /// <typeparam name="T">The return type.</typeparam>
    public class AsyncLazy<T> : Lazy<Task<T>>
    {
        /// <summary>Initializes a new instance of the <see cref="AsyncLazy{T}" /> class.</summary>
        /// <param name="valueFactory">The value factory.</param>
        public AsyncLazy(Func<T> valueFactory) :
            base(() => Task.Factory.StartNew(valueFactory))
        { }

        /// <summary>Initializes a new instance of the <see cref="AsyncLazy{T}" /> class.</summary>
        /// <param name="taskFactory">The task factory.</param>
        public AsyncLazy(Func<Task<T>> taskFactory) :
            base(() => Task.Factory.StartNew(taskFactory).Unwrap())
        { }

        /// <summary>Gets the awaiter.</summary>
        /// <returns>TaskAwaiter&lt;T&gt;.</returns>
        public TaskAwaiter<T> GetAwaiter() => this.Value.GetAwaiter();
    }
}

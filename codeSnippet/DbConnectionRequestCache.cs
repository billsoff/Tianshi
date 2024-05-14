using System;
using System.Threading;

namespace MyNamespace
{
    public static class DbConnectionRequestCache
    {
        private const string CONNECTION_KEY = "_db_connection";
        private static LocalDataStoreSlot _localSlot;

        static DbConnectionRequestCache()
        {
            _localSlot = Thread.AllocateDataSlot();
        }

        public static IDisposable Init(HttpContext httpContext)
        {
            Thread.SetData(_localSlot, httpContext.Items);

            return new DisposableObject();
        }

        public static IDbConnection CreateOrGetFromCache(Func<IDbConnection> connectionCreator)
        {
            IDbConnection connection = GetCachedDbConnection();

            if ((connection == null) || (connection.State != ConnectionState.Open))
            {
                connection = connectionCreator();
                connection.Open();
            }

            CacheConnection(connection);

            return connection
        }

        public static void DetermineDisposing(this IDbConnection connection)
        {
            return new DbConnectionDisposable(connection);
        }

        private static IDbConnection GetCachedDbConnection()
        {
            IDictionary<object, object?> cache = GetRequestCache();

            if (cache == null)
            {
                return null;
            }

            return (IDbConnection)cache[CONNECTION_KEY];
        }

        private static bool CacheConnection(IDbConnection connection)
        {
            IDictionary<object, object?> cache = GetRequestCache();

            if (cache == null)
            {
                return false;
            }

            cache[CONNECTION_KEY] = connection;

            return true;           
        }

        private static IDictionary<object, object?> GetRequestCache()
        {
            return (IDictionary<object, object?>)Thread.GetData(_localSlot);
        }


        private sealed class DisposableObject : IDisposable
        {
            public void Dispose()
            {
                IDbConnection connection = GetCachedDbConnection();
                connection?.Disponse();

                GC.SuppressFinalize(this);
            }
        }

        private sealed class DbConnectionDisposable : IDisposable
        {
            private readonly IDbConnection _connection;

            public DbConnectionDisposable(IDbConnection connection)
            {
                _connection = connection;
            }

            public void Dispose()
            {
                if (!ReferenceEquals(_connection, GetCachedDbConnection()))
                {
                    _connection?.Dispose();
                }

                GC.SuppressFinalize(this);
            }
        }
    }
}


// 1. Controller action head
using var _ = DbConnectionRequestCache.Init(HttpContext);


// 2. repository when create connection
return DbConnectionRequestCache.CreateOrGetFromCache(() => new SqlConnection(...));

// 3. modification of using when use DB connection

// original
using (var connection = _context.CreateConnect())

// modified
using MyNamespace;

// ...

IDbConnection connection;

using ((connection = _context.CreateConnect()).DetermineDisposing())

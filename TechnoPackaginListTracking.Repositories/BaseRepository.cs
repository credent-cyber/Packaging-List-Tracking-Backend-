using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechnoPackaginListTracking.Repositories
{
    public class BaseRepository
    {
        public BaseRepository(ILogger<BaseRepository> logger)
        {
            Logger = logger;
        }

        public ILogger Logger { get; private set; }
    }
}

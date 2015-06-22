using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BacASableWPF4
{
    public class LogLine
    {
        public LogLine(string line)
        {
            //1171690	2015-06-22 13:49:55,643	[8]	DEBUG	dsn-srv1-test	Cegid.DsnLink.DataAccess.Databases.Sql.RunSqlCommandService	-	Execute commande sql 'SELECT Id, DatabaseName, AggregateName, AggregateId, AggregateVersion, EventName, CreatedAt, StockageMode, DomainEvent as Data FROM Events WHERE DatabaseName = @databaseName AND AggregateName = @aggregateName AND AggregateId = @aggregateId' with parameters '{ databaseName = Environnement-c1fced7d-9282-4f58-bcb7-8d7f205b0d50, aggregateName = DeclarationSuiviEntity, aggregateId = 2015-02-19732209007159 }'
            var blocks = line.Split('\t');
            var dateBlocks = blocks[1].Split(' ');

            Id = long.Parse(blocks[0]);
            Date = DateTime.Parse(dateBlocks[0]).Add(TimeSpan.Parse(dateBlocks[1]));
            Thread = int.Parse(blocks[2].Replace("[", "").Replace("]", ""));
            Level = blocks[3];
            Server = blocks[4];
            Class = blocks[5];
            // blocks[6] == "-"
            Message = string.Join("\t", blocks.Skip(7));
        }

        public long Id { get; private set; }

        public DateTime Date { get; private set; }

        public int Thread { get; private set; }

        public string Level { get; private set; }

        public string Server { get; private set; }

        public string Class { get; private set; }

        public string Message { get; private set; }
    }
}

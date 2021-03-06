using System.Linq.Expressions;
using EFSqlTranslator.Translation.DbObjects;

namespace EFSqlTranslator.Translation.MethodTranslators
{
    public class WhereTranslator : AbstractMethodTranslator
    {
        public WhereTranslator(IModelInfoProvider infoProvider, IDbObjectFactory dbFactory) 
            : base(infoProvider, dbFactory)
        {
        }

        public override void Register(TranslationPlugIns plugIns)
        {
            plugIns.RegisterMethodTranslator("where", this);
        }

        public override void Translate(MethodCallExpression m, TranslationState state, UniqueNameGenerator nameGenerator)
        {
            var whereClause = (IDbBinary)state.ResultStack.Pop();
            var dbSelect = (IDbSelect)state.ResultStack.Peek();
            dbSelect.UpdateWhereClause(whereClause, _dbFactory);
        }   
    }
}
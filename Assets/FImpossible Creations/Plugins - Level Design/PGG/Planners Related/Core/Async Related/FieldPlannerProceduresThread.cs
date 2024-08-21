using FIMSpace.FTex;

namespace FIMSpace.Generating.Planning
{
    public partial class PlannerAsyncManager
    {
        public class FieldPlannerProceduresThread : FThread
        {
            FieldPlanner planner;
            PlannerAsyncManager manager;
            bool isPostProcedures;
            bool isMidProcedures;
            public FieldPlannerProceduresThread(PlannerAsyncManager manager, FieldPlanner planner, bool midProcedures, bool postProcedures )
            {
                this.manager = manager;
                this.planner = planner;
                isMidProcedures = midProcedures;
                isPostProcedures = postProcedures;
            }

            protected override void ThreadOperations()
            {
                if ( isMidProcedures)
                {
                    planner.RunMidProcedures(manager.Planner.LatestGenerated);
                    return;
                }

                if (!isPostProcedures)
                    planner.RunStartProcedures(manager.Planner.LatestGenerated);
                else
                    planner.RunPostProcedures(manager.Planner.LatestGenerated);
            }

        }
    }
}

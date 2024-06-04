using BookingApp_Backend.Helpers.Jobs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HangFire.Server.Jobs 
{
    internal class DeleteDataJob : IDeleteDataJob
    {   
        private readonly IDeleteOldData _deleteOldData;


        public DeleteDataJob(IDeleteOldData deleteOldData)
        {
            _deleteOldData = deleteOldData;
        }

        public async Task Execute()
        {
            await _deleteOldData.DeleteData();
        }
    }
}

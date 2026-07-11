using MVCGrid.Abstractions;
using MVCGrid.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MVCGrid.Interfaces
{
    public interface IMVCGridRenderingEngine
    {
        bool AllowsPaging { get; }
        void PrepareResponse(IGridResponse response);
        void Render(RenderingModel model, GridContext gridContext, TextWriter outputStream);
        void RenderContainer(ContainerRenderingModel model, TextWriter outputStream);
    }
}

FROM mcr.microsoft.com/dotnet/sdk:6.0-alpine

WORKDIR /workspace

COPY ChaChaOptimization/ /workspace/ChaChaOptimization/

RUN apk add --no-cache bash

WORKDIR /workspace/ChaChaOptimization/ChaChaOptimization

CMD ["/bin/bash"]
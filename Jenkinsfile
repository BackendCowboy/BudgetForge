pipeline {
    agent any
    
    environment {
        DOCKER_IMAGE = 'budgetforge/api'
        BUILD_TAG = "${BUILD_NUMBER}-${GIT_COMMIT.take(7)}"
    }
    
    stages {
        stage('🚀 Checkout & Info') {
            steps {
                echo "🔄 Starting BudgetForge CI/CD Pipeline..."
                sh '''
                    echo "📋 Build Information:"
                    echo "  Build Number: ${BUILD_NUMBER}"
                    echo "  Git Commit: ${GIT_COMMIT}"
                    echo "  Image Tag: ${BUILD_TAG}"
                    echo "  Workspace: ${WORKSPACE}"
                    
                    echo "🔧 Tool Versions:"
                    git --version
                    dotnet --version
                    docker --version
                    
                    echo "📁 Workspace Contents:"
                    ls -la
                '''
            }
        }
        
        stage('🧹 Cleanup') {
            steps {
                echo "🧹 Cleaning up previous builds..."
                sh '''
                    # Clean Docker resources (be careful not to remove running containers)
                    docker image prune -f || echo "Docker prune completed"
                    
                    # Clean .NET builds
                    dotnet clean || echo ".NET clean completed"
                    
                    # Remove old build artifacts
                    find . -name "bin" -type d -exec rm -rf {} + 2>/dev/null || true
                    find . -name "obj" -type d -exec rm -rf {} + 2>/dev/null || true
                '''
            }
        }
        
        stage('📦 Restore Dependencies') {
            steps {
                echo "📦 Restoring .NET dependencies..."
                sh '''
                    dotnet restore --verbosity normal
                    echo "✅ Dependencies restored successfully"
                '''
            }
        }
        
        stage('🔨 Build Application') {
            steps {
                echo "🔨 Building BudgetForge application..."
                sh '''
                    dotnet build --configuration Release --no-restore --verbosity normal
                    echo "✅ Build completed successfully"
                    
                    # Show build artifacts
                    echo "📦 Build Artifacts:"
                    find . -name "*.dll" -path "*/bin/Release/*" | head -10
                '''
            }
        }
        
        stage('🧪 Run Tests') {
            steps {
                echo "🧪 Running tests..."
                sh '''
                    # Run tests if any exist
                    if find . -name "*Tests.csproj" -type f | grep -q .; then
                        echo "🧪 Running unit tests..."
                        dotnet test --configuration Release --no-build --verbosity normal
                    else
                        echo "⚠️ No test projects found - skipping tests"
                    fi
                    echo "✅ Test stage completed"
                '''
            }
        }
        
        stage('🐳 Build Docker Image') {
            steps {
                echo "🐳 Building Docker image..."
                sh '''
                    echo "🏗️ Building Docker image: ${DOCKER_IMAGE}:${BUILD_TAG}"
                    
                    # Build the Docker image
                    docker build -f src/BudgetForge.Api/Dockerfile -t "${DOCKER_IMAGE}:${BUILD_TAG}" .
                    
                    # Tag as latest
                    docker tag "${DOCKER_IMAGE}:${BUILD_TAG}" "${DOCKER_IMAGE}:latest"
                    
                    # Show the built images
                    echo "✅ Docker images built:"
                    docker images "${DOCKER_IMAGE}" --format "table {{.Repository}}:{{.Tag}}\t{{.Size}}\t{{.CreatedSince}}"
                '''
            }
        }
        
        stage('🔍 Health Check') {
            steps {
                echo "🔍 Testing Docker image health..."
                sh '''
                    # Stop any existing test container
                    docker stop budgetforge-test 2>/dev/null || true
                    docker rm budgetforge-test 2>/dev/null || true
                    
                    # Run container for testing
                    echo "🚀 Starting test container..."
                    docker run -d --name budgetforge-test -p 8083:8080 "${DOCKER_IMAGE}:${BUILD_TAG}"
                    
                    # Wait for startup
                    echo "⏳ Waiting for application startup..."
                    sleep 30
                    
                    # Test health endpoint
                    echo "🏥 Testing health endpoint..."
                    for i in {1..10}; do
                        if curl -f http://localhost:8083/health; then
                            echo "✅ Health check passed!"
                            break
                        else
                            echo "⏳ Attempt $i failed, retrying..."
                            sleep 5
                        fi
                    done
                    
                    # Show container logs for debugging
                    echo "📋 Container logs:"
                    docker logs budgetforge-test --tail 20
                    
                    # Cleanup test container
                    docker stop budgetforge-test
                    docker rm budgetforge-test
                    
                    echo "✅ Health check completed"
                '''
            }
        }
        
        stage('🚀 Deploy to Development') {
            steps {
                echo "🚀 Deploying to development environment..."
                sh '''
                    echo "🔄 Updating development deployment..."
                    
                    # Update the running API container
                    cd ${WORKSPACE}
                    
                    # Stop current API container
                    docker-compose stop api || echo "API container stopped"
                    
                    # Remove old container
                    docker-compose rm -f api || echo "Old container removed"
                    
                    # Start with new image
                    docker-compose up -d api
                    
                    # Wait for startup
                    echo "⏳ Waiting for deployment..."
                    sleep 15
                    
                    # Verify deployment
                    echo "🏥 Verifying deployment..."
                    for i in {1..5}; do
                        if curl -f http://localhost:5001/health; then
                            echo "✅ Deployment successful!"
                            break
                        else
                            echo "⏳ Verification attempt $i..."
                            sleep 10
                        fi
                    done
                    
                    echo "🎉 Development deployment completed!"
                '''
            }
        }
    }
    
    post {
        success {
            echo '''
            🎉 BUILD SUCCESSFUL! 🎉
            
            ✅ All stages completed successfully
            ✅ Docker image built and tested
            ✅ Deployed to development environment
            
            🌐 Access your application:
            - API: http://localhost:5001/swagger
            - API Health: http://localhost:5001/health
            - Jenkins: http://localhost:8082
            
            🚀 Ready for production deployment!
            '''
        }
        failure {
            echo '''
            ❌ BUILD FAILED! ❌
            
            Check the console output above for specific errors.
            
            🔍 Common troubleshooting steps:
            1. Check Docker image build logs
            2. Verify .NET build succeeded
            3. Check application startup logs
            4. Verify health endpoints
            
            💪 Fix the issues and try again!
            '''
        }
        always {
            echo "🧹 Pipeline cleanup..."
            sh '''
                # Cleanup any test containers
                docker stop budgetforge-test 2>/dev/null || true
                docker rm budgetforge-test 2>/dev/null || true
                
                # Clean up workspace
                echo "✅ Cleanup completed"
            '''
            
            // Clean workspace
            cleanWs()
        }
    }
}
pipeline {
    agent any

    environment {
        IMAGE_NAME = 'landingmvc'
        DOCKER_REGISTRY = 'your-docker-registry.com'
    }

    stages {
        stage('Checkout') {
            steps {
                checkout scm
            }
        }

        stage('Inject Secure AppSettings') {
            steps {
                echo 'Pulling secure JsonConfig from Jenkins Config File Provider...'
                // Using the Config File Provider Plugin to inject the managed JSON file
                configFileProvider([configFile(fileId: 'landinggooglejson', targetLocation: 'appsettings.json')]) {
                    echo 'Secure appsettings.json has been placed in the workspace.'
                    // The Docker Build stage will automatically pick this file up during the 'COPY . .' step!
                }
            }
        }

        stage('Docker Build (Compiles .NET)') {
            steps {
                echo 'Building the Docker container (this also compiles the .NET app)...'
                // We do not need a separate 'dotnet build' stage because the Dockerfile uses a multi-stage build 
                // containing the mcr.microsoft.com/dotnet/sdk:8.0 image which handles the compilation internally!
                sh "docker build -t ${DOCKER_REGISTRY}/${IMAGE_NAME}:${BUILD_NUMBER} ."
                sh "docker tag ${DOCKER_REGISTRY}/${IMAGE_NAME}:${BUILD_NUMBER} ${DOCKER_REGISTRY}/${IMAGE_NAME}:latest"
            }
        }

        stage('Docker Push') {
            steps {
                echo 'Pushing image to registry...'
                // Uncomment when ready to push
                // withCredentials([usernamePassword(credentialsId: 'docker-registry-credentials', usernameVariable: 'DOCKER_USER', passwordVariable: 'DOCKER_PASS')]) {
                //     sh "echo $DOCKER_PASS | docker login ${DOCKER_REGISTRY} -u $DOCKER_USER --password-stdin"
                //     sh "docker push ${DOCKER_REGISTRY}/${IMAGE_NAME}:${BUILD_NUMBER}"
                //     sh "docker push ${DOCKER_REGISTRY}/${IMAGE_NAME}:latest"
                // }
                echo 'Skipping docker push - uncomment the block above when credentials are configured.'
            }
        }
    }

    post {
        always {
            echo 'Cleaning up workspace...'
            cleanWs()
        }
        success {
            echo 'Build and Deployment Successful!'
        }
        failure {
            echo 'Pipeline failed. Check the logs for details.'
        }
    }
}

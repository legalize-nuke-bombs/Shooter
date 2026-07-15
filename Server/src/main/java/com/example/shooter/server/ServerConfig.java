package com.example.shooter.server;

import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;

@Configuration
public class ServerConfig {

    @Bean
    public ServerRepresentation serverRepresentation() {
        return new ServerRepresentation(
            "Shooter Server 0",
                0,
                0,
                0
        );
    }
}

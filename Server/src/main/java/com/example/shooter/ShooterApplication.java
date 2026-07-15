package com.example.shooter;

import org.springframework.boot.SpringApplication;
import org.springframework.boot.autoconfigure.SpringBootApplication;
import org.springframework.scheduling.annotation.EnableScheduling;

@EnableScheduling
@SpringBootApplication
public class ShooterApplication {

    public static void main(String[] args) {

        SpringApplication.run(ShooterApplication.class, args);

    }

}

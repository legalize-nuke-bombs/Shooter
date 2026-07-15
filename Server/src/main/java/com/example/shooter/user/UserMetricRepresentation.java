package com.example.shooter.user;

import lombok.AllArgsConstructor;
import lombok.Getter;

@Getter
@AllArgsConstructor
public class UserMetricRepresentation {
    private final Long totalUsers;
    private final Long totalUsers24h;
}

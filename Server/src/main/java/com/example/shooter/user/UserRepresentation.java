package com.example.shooter.user;

import lombok.Getter;

@Getter
public class UserRepresentation {
    private final Long id;
    private final String displayName;

    public UserRepresentation(User user) {
        this.id = user.getId();
        this.displayName = user.getDisplayName();
    }
}
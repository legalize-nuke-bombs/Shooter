package com.example.shooter.game.world;

import com.fasterxml.jackson.annotation.JsonInclude;
import io.swagger.v3.oas.annotations.media.Schema;
import lombok.AllArgsConstructor;
import lombok.Getter;

import java.util.UUID;

@Getter
@AllArgsConstructor
@JsonInclude(JsonInclude.Include.NON_NULL)
@Schema(description = """
        Если указаны оба поля, Unity Server должен выгнать указанного пользователя из указанного мира.
        Если указан только пользователь, Unity Server должен выгнать указанного пользователя из всех миров.
        Если указан только мир, Unity Server должен выгнать всех пользователей из указанного мира.
        
        Unity Server должен отклонять любые попытки установить соединение
        по токенам доступа в мир, выданным до кика
        """)
public class UnityHook {
    private final Long userIdToKick;
    private final UUID worldIdToKick;
}
